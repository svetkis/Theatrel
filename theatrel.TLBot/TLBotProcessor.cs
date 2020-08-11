using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using theatrel.TLBot.Commands;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    public class TLBotProcessor : ITLBotProcessor
    {
        private ITLBotService _botService;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly IDictionary<long, IChatDataInfo> _chatsInfo = new ConcurrentDictionary<long, IChatDataInfo>();

        private readonly AppDbContext _dbContext;

        public TLBotProcessor(IFilterHelper filterHelper, AppDbContext dbContext, IPlayBillDataResolver playBillResolver)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _dbContext = dbContext;

            _commands.Add(new StartCommand(dbContext));
            _commands.Add(new MonthCommand());
            _commands.Add(new DaysOfWeekCommand());
            _commands.Add(new PerformanceTypesCommand());
            _commands.Add(new GetPerformancesCommand(playBillResolver, filterHelper));
        }

        public void Start(ITLBotService botService, CancellationToken cancellationToken)
        {
            _botService = botService;
            _botService.OnMessage += OnMessage;
            _botService.Start(cancellationToken);
        }

        public void Stop()
        {
            _botService?.Stop();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        ~TLBotProcessor()
        {
            _botService?.Stop();
        }

        private ChatInfoEntity GetChatInfo(long chatId)
        {
            try
            {
                return _dbContext.TlChats.FirstOrDefault(u => u.ChatId == chatId);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
            }

            return null;
        }

        private async Task AddChatInfo(ChatInfoEntity chatInfo, CancellationToken cancellationToken)
        {
            try
            {
                var chatInfoItem = _dbContext.TlChats.FirstOrDefault(u => u.ChatId == chatInfo.ChatId);
                if (chatInfoItem != null)
                    _dbContext.TlChats.Remove(chatInfoItem);

                _dbContext.TlChats.Add(chatInfo);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
            }
        }

        private readonly List<IDialogCommand> _commands = new List<IDialogCommand>();

        private async void OnMessage(object sender, ITLMessage tLMessage)
        {
            Trace.TraceInformation($"{tLMessage.ChatId} {tLMessage.Message}");
            string message = tLMessage.Message;
            long chatId = tLMessage.ChatId;

            ChatInfoEntity chatInfo = GetChatInfo(chatId);
            if (null == chatInfo)
            {
                chatInfo = new ChatInfoEntity {ChatId = chatId, Culture = "ru"};
                await AddChatInfo(chatInfo, _cancellationTokenSource.Token);
            }

            chatInfo.LastMessage = DateTime.Now;

            //check if user wants to return at first
            var startCmd = _commands.First();
            if (startCmd.IsMessageCorrect(message))
                chatInfo.Clear();

            var prevCmd = GetPreviousCommand(chatInfo);

            if (message.ToLower().StartsWith("нет") || (prevCmd?.IsReturnCommand(message) ?? false))
            {
                var prevCommand = GetPreviousCommand(chatInfo);

                if (prevCommand == null)
                    return;

                chatInfo.DialogState = DialogStateEnum.DialogReturned;
                chatInfo.CurrentStepId -= 1;
                chatInfo.PreviousStepId -= 1;

                await CommandAskQuestion(prevCommand, chatInfo, null);

                return;
            }

            IDialogCommand command = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId);
            if (!command.IsMessageCorrect(message))
            {
                SendWrongCommandMessage(chatId, message, chatInfo.CurrentStepId);
                return;
            }

            ICommandResponse acknowledgement = await command.ApplyResultAsync(chatInfo, message, _cancellationTokenSource.Token);

            chatInfo.PreviousStepId = chatInfo.CurrentStepId;
            ++chatInfo.CurrentStepId;

            var nextCommand = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId);
            await CommandAskQuestion(nextCommand, chatInfo, acknowledgement);
        }

        private IDialogCommand GetPreviousCommand(IChatDataInfo chatInfo)
        {
            return chatInfo.PreviousStepId == -1
                ? null
                : _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.PreviousStepId);
        }

        private IDialogCommand GetNextCommand(IChatDataInfo chatInfo)
            => _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.CurrentStepId + 1);


        private async Task CommandAskQuestion(IDialogCommand cmd, IChatDataInfo chatInfo, ICommandResponse previousCmdAcknowledgement)
        {
            if (cmd == null)
                return;

            ICommandResponse nextDlgQuestion = await cmd.AscUserAsync(chatInfo, _cancellationTokenSource.Token);
            ICommandResponse botResponse = nextDlgQuestion;
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

            if (null == GetNextCommand(chatInfo))
            {
                if (chatInfo is ChatInfoEntity info)
                    _dbContext.TlChats.Remove(info);
            }

            await _dbContext.SaveChangesAsync();
        }

        private void SendWrongCommandMessage(long chatId, string message, int chatLevel)
        {
            Trace.TraceInformation($"Wrong command: {chatId} {chatLevel} {message}");
           _botService.SendMessageAsync(chatId, new TlCommandResponse("Простите, я вас не понял. Попробуйте еще раз."));
        }
    }
}
