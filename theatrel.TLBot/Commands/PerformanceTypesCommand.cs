using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class PerformanceTypesCommand : DialogCommandBase
    {
        private readonly string[] _types = { "опера", "балет", "концерт" };
        private readonly string[] _every = { "Все", "всё", "любой", "любое", "не важно"};

        protected override string ReturnCommandMessage { get; set; } = "Выбрать другое";

        public override string Name => "Выбрать тип представления";
        public PerformanceTypesCommand() : base((int) DialogStep.SelectType)
        {
            var buttons = _types.Select(m => new KeyboardButton(m)).Concat(new []{ new KeyboardButton(_every.First()) }).ToArray();

            CommandKeyboardMarkup = new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(buttons, ButtonsInLine),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }

        public override async Task<ICommandResponse> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            chatInfo.Types = ParseMessage(message);

            return new TlCommandResponse(null);
        }

        public override bool IsMessageCorrect(string message) => SplitMessage(message).Any();

        public override async Task<ICommandResponse> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Какие представления Вас интересуют?");

            return new TlCommandResponse(stringBuilder.ToString(), CommandKeyboardMarkup);
        }

        private string[] ParseMessage(string message)
        {
            var parts = SplitMessage(message);
            if (parts.Any(p => _every.Any(e => e.ToLower().Contains(p.ToLower()))))
                return null;

            return parts.Select(ParseMessagePart).Where(idx => idx > -1).Select(idx => _types[idx]).ToArray();
        }

        private string[] SplitMessage(string message) => message.Split(WordSplitters).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        private int ParseMessagePart(string messagePart)
        {
            if (string.IsNullOrWhiteSpace(messagePart))
                return -1;

            return CheckEnumerable(_types, messagePart);
        }

        private int CheckEnumerable(string[] checkedData, string msg)
        {
            var data = checkedData.Select((item, idx) => new { idx, item })
                .FirstOrDefault(data => 0 == string.Compare(data.item, msg, StringComparison.OrdinalIgnoreCase));

            return data?.idx ?? -1;
        }
    }
}
