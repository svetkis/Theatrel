using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.TLBot.Commands;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    public class TLBotProcessor : ITLBotProcessor
    {
        private ITLBotService _botService;

        private IDictionary<long, IChatDataInfo> _chatsInfo = new ConcurrentDictionary<long, IChatDataInfo>();

        public TLBotProcessor(IFilterHelper filterhelper, IPlayBillDataResolver playBillResolver)
        {
            _commands.Add(new StartCommand());
            _commands.Add(new MonthCommand());
            _commands.Add(new DaysOfWeekCommand());
            _commands.Add(new PerfomanceTypesCommand());
            _commands.Add(new GetPerfomancesCommand(playBillResolver, filterhelper));
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
        }

        private IChatDataInfo GetChatInfo(long chatId)
        {
            if (!_chatsInfo.ContainsKey(chatId))
                _chatsInfo[chatId] = new ChatDataInfo() { ChatId = chatId, Culture = "ru"};

            return _chatsInfo[chatId];
        }

        private List<IDialogCommand> _commands = new List<IDialogCommand>();

        private void OnMessage(object sender, ITLMessage tLMessage)
        {
            Trace.TraceInformation($"{tLMessage.ChatId} {tLMessage.Message}");
            string message = tLMessage.Message;
            long chatId = tLMessage.ChatId;

            IChatDataInfo chatInfo = GetChatInfo(chatId);
            chatInfo.LastMessage = DateTime.Now;

            //check if user wants to return at first
            var startCmd = _commands.First();
            if (startCmd.IsMessageClear(message))
                chatInfo.ChatStep = 0;

            IDialogCommand command = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep);

            if (message.ToLower().StartsWith("нет"))
            {
                if (chatInfo.ChatStep > 0)
                    --chatInfo.ChatStep;

                CommandAskQuestion(command, chatInfo, null);
                return;
            }

            if (!command.IsMessageClear(message))
            {
                SendWrongCommandMessage(chatId, message, chatInfo.ChatStep);
                return;
            }

            string acknowledgement = command.ApplyResult(chatInfo, message);

            ++chatInfo.ChatStep;

            var nextCommand = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep);
            CommandAskQuestion(nextCommand, chatInfo, acknowledgement);
        }

        private void CommandAskQuestion(IDialogCommand cmd, IChatDataInfo chatInfo, string previousCmdAcknowledgement)
        {
            if (cmd == null)
                return;

            string nextDlgQuestion = cmd.ExecuteAsync(chatInfo).GetAwaiter().GetResult();
            string botResponse = string.IsNullOrWhiteSpace(previousCmdAcknowledgement)
                ? nextDlgQuestion
                : $"{previousCmdAcknowledgement}{Environment.NewLine}{nextDlgQuestion}";

            Task.Run(() => _botService.SendMessageAsync(chatInfo.ChatId, botResponse));

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
