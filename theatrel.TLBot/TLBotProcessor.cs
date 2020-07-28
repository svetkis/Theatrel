using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess;
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

        public TLBotProcessor(IFilterHelper filterHelper, AppDbContext dbContext, IPlayBillDataResolver playBillResolver)
        {
            _cancellationTokenSource = new CancellationTokenSource();

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

        private IChatDataInfo GetChatInfo(long chatId)
        {
            if (!_chatsInfo.ContainsKey(chatId))
                _chatsInfo[chatId] = new ChatDataInfo { ChatId = chatId, Culture = "ru"};

            return _chatsInfo[chatId];
        }

        private readonly List<IDialogCommand> _commands = new List<IDialogCommand>();

        private async void OnMessage(object sender, ITLMessage tLMessage)
        {
            Trace.TraceInformation($"{tLMessage.ChatId} {tLMessage.Message}");
            string message = tLMessage.Message;
            long chatId = tLMessage.ChatId;

            IChatDataInfo chatInfo = GetChatInfo(chatId);
            chatInfo.LastMessage = DateTime.Now;

            //check if user wants to return at first
            var startCmd = _commands.First();
            if (startCmd.IsMessageReturnToStart(message))
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
            if (!command.IsMessageReturnToStart(message))
            {
                SendWrongCommandMessage(chatId, message, chatInfo.ChatStep);
                return;
            }

            ICommandResponse acknowledgement = await command.ApplyResultAsync(chatInfo, message, _cancellationTokenSource.Token);

            ++chatInfo.ChatStep;

            var nextCommand = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep);
            await CommandAskQuestion(nextCommand, chatInfo, acknowledgement);
        }

        private async Task CommandAskQuestion(IDialogCommand cmd, IChatDataInfo chatInfo, ICommandResponse previousCmdAcknowledgement)
        {
            if (cmd == null)
                return;

            ICommandResponse nextDlgQuestion = await cmd.AscUserAsync(chatInfo, _cancellationTokenSource.Token);
            ICommandResponse botResponse = nextDlgQuestion;
            if (!string.IsNullOrWhiteSpace(previousCmdAcknowledgement?.Message))
                botResponse.Message = $"{previousCmdAcknowledgement.Message}{Environment.NewLine}{nextDlgQuestion}";

            await Task.Run(() => _botService.SendMessageAsync(chatInfo.ChatId, botResponse), _cancellationTokenSource.Token);

            if (_commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep + 1) == null)
                chatInfo.ChatStep = (int)DialogStep.Start;
        }

        private void SendWrongCommandMessage(long chatId, string message, int chatLevel)
        {
            Trace.TraceInformation($"Wrong command: {chatId} {chatLevel} {message}");
           _botService.SendMessageAsync(chatId, new TlCommandResponse("Простите, я вас не понял. Попробуйте еще раз."));
        }
    }
}
