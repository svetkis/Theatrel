using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.TLBot.Commands;
using theatrel.TLBot.Entities;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    public class TLBotProcessor : ITLBotProcessor
    {
        private ITLBotService _botService;

        private readonly IDictionary<long, IChatDataInfo> _chatsInfo = new ConcurrentDictionary<long, IChatDataInfo>();
        private readonly ApplicationContext _db;

        public TLBotProcessor(IFilterHelper filterHelper, IPlayBillDataResolver playBillResolver)
        {
            _db = new ApplicationContext();

            _commands.Add(new StartCommand());
            _commands.Add(new MonthCommand());
            _commands.Add(new DaysOfWeekCommand());
            _commands.Add(new PerfomanceTypesCommand());
            _commands.Add(new GetPerfomancesCommand(playBillResolver, filterHelper));
        }

        public void Start(ITLBotService botService, CancellationToken cancellationToken)
        {
            _botService = botService;
            _botService.OnMessage += OnMessage;
            _botService.Start(cancellationToken);
        }

        public void Stop() => _botService?.Stop();

        ~TLBotProcessor()
        {
            _botService?.Stop();
            _db.Dispose();
        }

        private IChatDataInfo GetChatInfo(long chatId)
        {
            if (!_chatsInfo.ContainsKey(chatId))
                _chatsInfo[chatId] = new ChatDataInfo() { ChatId = chatId, Culture = "ru"};

            return _chatsInfo[chatId];
        }

        private readonly List<IDialogCommand> _commands = new List<IDialogCommand>();

        private async void OnMessage(object sender, ITLMessage tLMessage)
        {
            Trace.TraceInformation($"{tLMessage.ChatId} {tLMessage.Message}");
            string message = tLMessage.Message;
            long chatId = tLMessage.ChatId;

            if (!_db.TlUsers.Any(u => u.Id == chatId))
                await _db.TlUsers.AddAsync(new TlUser {Id = chatId});

            IChatDataInfo chatInfo = GetChatInfo(chatId);
            chatInfo.LastMessage = DateTime.Now;

            //check if user wants to return at first
            var startCmd = _commands.First();
            if (startCmd.IsMessageClear(message))
                chatInfo.Clear();

            if (message.ToLower().StartsWith("нет"))
            {
                if (chatInfo.ChatStep > 0)
                    --chatInfo.ChatStep;

                chatInfo.DialogState = DialogStateEnum.DialogReturned;

                var prevCommand = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep);
                await CommandAskQuestion(prevCommand, chatInfo, null);
                return;
            }

            IDialogCommand command = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep);
            if (!command.IsMessageClear(message))
            {
                SendWrongCommandMessage(chatId, message, chatInfo.ChatStep);
                return;
            }

            string acknowledgement = command.ApplyResult(chatInfo, message);

            ++chatInfo.ChatStep;

            var nextCommand = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep);
            await CommandAskQuestion(nextCommand, chatInfo, acknowledgement);
        }

        private async Task CommandAskQuestion(IDialogCommand cmd, IChatDataInfo chatInfo, string previousCmdAcknowledgement)
        {
            if (cmd == null)
                return;

            string nextDlgQuestion = await cmd.ExecuteAsync(chatInfo);
            string botResponse = string.IsNullOrWhiteSpace(previousCmdAcknowledgement)
                ? nextDlgQuestion
                : $"{previousCmdAcknowledgement}{Environment.NewLine}{nextDlgQuestion}";

            await Task.Run(() => _botService.SendMessageAsync(chatInfo.ChatId, botResponse));

            if (_commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep + 1) == null)
                chatInfo.ChatStep = (int)DialogStep.Start;
        }

        private void SendWrongCommandMessage(long chatId, string message, int chatLevel)
        {
            Trace.TraceInformation($"Wrong command: {chatId} {chatLevel} {message}");
           _botService.SendMessageAsync(chatId, "Простите, я вас не понял. Попробуйте еще раз.");
        }
    }
}
