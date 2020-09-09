using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using theatrel.Interfaces.TgBot;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Commands;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot
{
    internal class TgBotProcessor : ITgBotProcessor
    {
        private ITgBotService _botService;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly IDbService _dbService;

        private readonly List<IDialogCommand> _commands = new List<IDialogCommand>();

        public TgBotProcessor(IDbService dbService, IPlayBillDataResolver playBillResolver,
            IFilterService filterService, ITimeZoneService timeZoneService)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _dbService = dbService;

            _commands.Add(new StartCommand(dbService));
            _commands.Add(new MonthCommand(dbService));
            _commands.Add(new DaysOfWeekCommand(dbService));
            _commands.Add(new PerformanceTypesCommand(dbService));
            _commands.Add(new GetPerformancesCommand(filterService, timeZoneService, dbService));
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
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
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
            var startCmd = _commands.First();
            if (startCmd.IsMessageCorrect(message))
            {
                await EnsureDbUserExists(chatInfo.ChatId, chatInfo.Culture);
                chatInfo.Clear();
            }

            var prevCmd = GetPreviousCommand(chatInfo);

            //check if user wants to return one step back
            if (message.ToLower().StartsWith("нет") || (prevCmd?.IsReturnCommand(message) ?? false))
            {
                await ProcessOneStepBack(chatsRepository, chatInfo);
                return;
            }

            IDialogCommand command = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId);
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
            ITgOutboundMessage acknowledgement = await command.ApplyResult(chatInfo, message, _cancellationTokenSource.Token);

            chatInfo.PreviousStepId = chatInfo.CurrentStepId;
            ++chatInfo.CurrentStepId;
            var nextCommand = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId);
            if (nextCommand != null)
            {
                await chatsRepository.Update(chatInfo);
                await CommandAskQuestion(nextCommand, chatInfo, acknowledgement);
            }
            else
            {
                await Task.Run(() => _botService.SendMessageAsync(chatInfo.ChatId, acknowledgement),
                    _cancellationTokenSource.Token);
                await chatsRepository.Delete(chatInfo);
            }
        }

        private IDialogCommand GetPreviousCommand(IChatDataInfo chatInfo) =>
            chatInfo.PreviousStepId == -1
                ? null
                : _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.PreviousStepId);

        private IDialogCommand GetNextCommand(IChatDataInfo chatInfo) =>
            _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId + 1);

        private async Task CommandAskQuestion(IDialogCommand cmd, ChatInfoEntity chatInfo, ITgOutboundMessage previousCmdAcknowledgement)
        {
            if (cmd == null)
                return;

            ITgOutboundMessage nextDlgQuestion = await cmd.AscUser(chatInfo, _cancellationTokenSource.Token);
            ITgOutboundMessage botResponse = nextDlgQuestion;
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

            await Task.Run(() => _botService.SendMessageAsync(chatInfo.ChatId, botResponse), _cancellationTokenSource.Token);
        }

        private void SendWrongCommandMessage(long chatId, string message, int chatLevel)
        {
            Trace.TraceInformation($"Wrong command: {chatId} {chatLevel} {message}");
            _botService.SendMessageAsync(chatId, new TgOutboundMessage("Простите, я вас не понял. Попробуйте еще раз."));
        }

        private void SendErrorMessage(long chatId, string message, int chatLevel)
        {
            Trace.TraceInformation($"Wrong command: {chatId} {chatLevel} {message}");
            _botService.SendMessageAsync(chatId, new TgOutboundMessage("Простите, что то пошло не так. Попробуйте еще раз."));
        }

    }
}
