using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot
{
    internal class TgBotProcessor : ITgBotProcessor
    {
        private ITgBotService _botService;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken InternalCancellationToken => _cancellationTokenSource.Token;

        private readonly IDbService _dbService;

        private readonly IDialogCommand[][] _commands;

        public TgBotProcessor(IDbService dbService, ITgCommandsConfigurator configurator)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _dbService = dbService;
            _commands = configurator.GetDialogCommands();
        }

        public void Start(ITgBotService botService, CancellationToken cancellationToken)
        {
            if (_botService != null)
                throw new Exception("Service is already started.");

            _botService = botService;
            _botService.OnMessage += OnMessage;
            _botService.Start(cancellationToken);
        }

        public void Stop()
        {
            if (_botService == null)
                return;

            _botService.OnMessage -= OnMessage;
            _botService?.Stop();
            _botService = null;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        ~TgBotProcessor() => Stop();

        private async Task EnsureDbUserExists(long userId, string culture)
        {
            using var usersRepository = _dbService.GetUsersRepository();

            var userEntity = usersRepository.Get(userId);
            if (null == userEntity)
                await usersRepository.Create(userId, culture, _cancellationTokenSource.Token);
        }

        private async Task ProcessOneStepBack(ITgChatsRepository chatsRepository, ChatInfoEntity chatInfo)
        {
            var prevCommand = GetPreviousCommand(chatInfo);

            if (prevCommand == null)
                return;

            chatInfo.DialogState = DialogStateEnum.DialogReturned;
            chatInfo.CurrentStepId -= 1;
            chatInfo.PreviousStepId -= 1;

            await CommandAskQuestion(prevCommand, chatInfo, null);

            if (null == GetNextCommand(chatInfo))
                await chatsRepository.Delete(chatInfo);
            else
                await chatsRepository.Update(chatInfo);
        }

        private async void OnMessage(object sender, ITgInboundMessage tLInboundMessage)
        {
            Trace.TraceInformation($"{tLInboundMessage.ChatId} {tLInboundMessage.Message}");
            MemoryHelper.LogMemoryUsage();

            string message = tLInboundMessage.Message;
            long chatId = tLInboundMessage.ChatId;

            using ITgChatsRepository chatsRepository = _dbService.GetChatsRepository();

            ChatInfoEntity chatInfo = await chatsRepository.Get(chatId)
                                      ?? await chatsRepository.Create(chatId, "ru", _cancellationTokenSource.Token);

            chatInfo.LastMessage = DateTime.Now;

            //check if user wants to return to first step
            int idxFirstCorrectBlock = _commands.IndexWhere(commandBlock => commandBlock.First().IsMessageCorrect(message));
            if (-1 != idxFirstCorrectBlock)
            {
                await EnsureDbUserExists(chatInfo.UserId, chatInfo.Culture);
                chatInfo.Clear();
                chatInfo.CommandLine = idxFirstCorrectBlock;
            }

            var prevCmd = GetPreviousCommand(chatInfo);

            //check if user wants to return one step back
            if (message.ToLower().StartsWith("нет") || (prevCmd?.IsReturnCommand(message) ?? false))
            {
                await ProcessOneStepBack(chatsRepository, chatInfo);
                return;
            }

            IDialogCommand command = _commands[chatInfo.CommandLine].FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId);
            if (command == null)
            {
                Trace.TraceError($"Current command not found {chatInfo.CurrentStepId}");
                SendErrorMessage(chatId, message, chatInfo.CurrentStepId);
                await chatsRepository.Delete(chatInfo);
                return;
            }

            if (!command.IsMessageCorrect(message))
            {
                SendWrongCommandMessage(chatId, message, chatInfo.CurrentStepId);
                return;
            }

            Trace.TraceInformation($"Command {command.Name} ApplyResult");
            ITgCommandResponse acknowledgement = await command.ApplyResult(chatInfo, message, _cancellationTokenSource.Token);

            if (!acknowledgement.NeedToRepeat)
            {
                chatInfo.PreviousStepId = chatInfo.CurrentStepId;
                ++chatInfo.CurrentStepId;
            }

            var nextCommand = _commands[chatInfo.CommandLine].FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId);
            if (nextCommand != null)
            {
                await chatsRepository.Update(chatInfo);
                await CommandAskQuestion(nextCommand, chatInfo, acknowledgement);
            }
            else
            {
                await Task.Run(() => _botService.SendMessageAsync(chatInfo.UserId, acknowledgement, InternalCancellationToken),
                    InternalCancellationToken);
                await chatsRepository.Delete(chatInfo);
            }
        }

        private IDialogCommand GetPreviousCommand(IChatDataInfo chatInfo) =>
            chatInfo.PreviousStepId == -1
                ? null
                : _commands[chatInfo.CommandLine].FirstOrDefault(cmd => cmd.Label == chatInfo.PreviousStepId);

        private IDialogCommand GetNextCommand(IChatDataInfo chatInfo) =>
            _commands[chatInfo.CommandLine].FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId + 1);

        private async Task CommandAskQuestion(IDialogCommand cmd, ChatInfoEntity chatInfo, ITgCommandResponse previousCmdAcknowledgement)
        {
            if (cmd == null)
                return;

            ITgCommandResponse nextDlgQuestion = await cmd.AscUser(chatInfo, _cancellationTokenSource.Token);
            ITgCommandResponse botResponse = nextDlgQuestion;
            if (!string.IsNullOrWhiteSpace(previousCmdAcknowledgement?.Message))
                botResponse.Message = $"{previousCmdAcknowledgement.Message}{Environment.NewLine}{nextDlgQuestion.Message}";

            if (null != previousCmdAcknowledgement?.ReplyKeyboard)
            {
                botResponse.ReplyKeyboard = new ReplyKeyboardMarkup
                {
                    Keyboard = botResponse.ReplyKeyboard?.Keyboard != null
                        ? previousCmdAcknowledgement.ReplyKeyboard.Keyboard.Concat(botResponse.ReplyKeyboard.Keyboard)
                        : previousCmdAcknowledgement.ReplyKeyboard?.Keyboard,
                    OneTimeKeyboard = botResponse.ReplyKeyboard?.OneTimeKeyboard ?? true,
                    ResizeKeyboard = botResponse.ReplyKeyboard?.ResizeKeyboard ?? true
                };
            }

            await Task.Run(() => _botService.SendMessageAsync(chatInfo.UserId, botResponse, InternalCancellationToken));
        }

        private void SendWrongCommandMessage(long chatId, string message, int chatLevel)
        {
            Trace.TraceInformation($"Wrong command: {chatId} {chatLevel} {message}");
            _botService.SendMessageAsync(chatId, new TgCommandResponse("Простите, я вас не понял. Попробуйте еще раз."), InternalCancellationToken);
        }

        private void SendErrorMessage(long chatId, string message, int chatLevel)
        {
            Trace.TraceInformation($"Wrong command: {chatId} {chatLevel} {message}");
            _botService.SendMessageAsync(chatId, new TgCommandResponse("Простите, что то пошло не так. Попробуйте еще раз."), InternalCancellationToken);
        }
    }
}
