using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public void Start(ITLBotService botService)
        {
            _botService = botService;
            _botService.OnMessage += OnMessage;
            _botService.Start();
        }

        ~TLBotProcessor()
        {
            _botService?.Stop();
        }

        private IChatDataInfo GetChatInfo(long chatId)
        {
            if (!_chatsInfo.ContainsKey(chatId))
                _chatsInfo[chatId] = new ChatDataInfo();

            return _chatsInfo[chatId];
        }

        private List<IDialogCommand> _commands = new List<IDialogCommand>();

        private void OnMessage(object sender, ITLMessage tLMessage)
        {
            string message = tLMessage.Message;
            long chatId = tLMessage.ChatId;

            IChatDataInfo chatInfo = GetChatInfo(chatId);
            chatInfo.LastMessage = DateTime.Now;

            IDialogCommand command = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep);

            if (!command.CanExecute(message))
            {
                SendWrongCommandMessage(chatId, message, chatInfo.ChatStep);
                return;
            }

            command.ApplyResult(chatInfo, message);
            ++chatInfo.ChatStep;

            var nextCommand = _commands.FirstOrDefault(cmd => cmd.Label == chatInfo.ChatStep);
            if (nextCommand != null)
            {
                string botResponse = nextCommand.ExecuteAsync(chatInfo).GetAwaiter().GetResult();
                Task.Run(() => _botService.SendMessageAsync(chatId, botResponse));
                return;
            }

            chatInfo.ChatStep = (int) DialogStep.Start;
        }

        private void SendWrongCommandMessage(long chatId, string message, int chatLevel)
        {
            Trace.TraceInformation($"Wrong command: {chatId} {chatLevel} {message}");
           _botService.SendMessageAsync(chatId, "Простите, я вас не понял. Попробуйте еще раз.");
        }
    }
}
