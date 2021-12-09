using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands
{
    internal class LocationCommand : DialogCommandBase
    {
        private const string GoodDay = "Добрый день! ";
        private const string IWillHelpYou = "Я помогу вам подобрать билеты в Мариинский или Михайловский театр. ";
        private const string Msg = "Какую площадку вы желаете посетить?";

        private readonly string[] _types;
        private readonly string[] _every = { "Любую", "Все", "всё", "любой", "любое", "не важно" };

        protected override string ReturnCommandMessage { get; set; } = "Выбрать другую площадку";

        public override string Name => "Выбрать площадку";
        public LocationCommand(IDbService dbService) : base(dbService)
        {
            using var repo = dbService.GetPlaybillRepository();
            _types = repo.GetLocationsList().Select(l => l.Name).ToArray();

            var buttons = _types.Select(m => new KeyboardButton(m)).Concat(new[] { new KeyboardButton(_every.First()) }).ToArray();

            CommandKeyboardMarkup = new ReplyKeyboardMarkup(GroupKeyboardButtons(ButtonsInLine, buttons))
            {
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }

        public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            chatInfo.Locations = ParseMessage(message);

            var selected = chatInfo.Locations != null ? string.Join(" или ", chatInfo.Locations) : "любую площадку";
            return Task.FromResult<ITgCommandResponse>(
                new TgCommandResponse($"{YouSelected} {selected}. {ReturnMsg}", ReturnCommandMessage));
        }

        public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => SplitMessage(message).Any();

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            return chatInfo.DialogState switch
            {
                DialogStateEnum.DialogReturned => Task.FromResult<ITgCommandResponse>(
                    new TgCommandResponse(Msg, CommandKeyboardMarkup)),
                DialogStateEnum.DialogStarted => Task.FromResult<ITgCommandResponse>(
                    new TgCommandResponse($"{GoodDay}{IWillHelpYou}{Msg}", CommandKeyboardMarkup)),
                _ => throw new NotImplementedException()
            };
        }

        private string[] ParseMessage(string message)
        {
            string[] parts = SplitMessage(message).ToArray();

            if (parts.Any(p => int.TryParse(p, out int idx) && idx < _types.Length))
            {
                return parts.Where(p => int.TryParse(p, out int idx) && idx < _types.Length && idx > 0)
                            .Select(p => _types[int.Parse(p) - 1]).ToArray();
            }

            if (parts.Any(p => _every.Any(e => e.ToLower().Contains(p.ToLower()))))
                return null;

            return parts.Select(ParseMessagePart).Where(idx => idx > -1).Select(idx => _types[idx]).ToArray();
        }

        private static string[] SplitMessage(string message)
            => message.Split(",")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(s => s.Trim())
            .ToArray();

        private int ParseMessagePart(string messagePart)
        {
            if (string.IsNullOrWhiteSpace(messagePart))
                return -1;

            return CheckEnumerable(_types, messagePart);
        }

        private static int CheckEnumerable(string[] checkedData, string msg)
        {
            var data = checkedData.Select((item, idx) => new { idx, item })
                .FirstOrDefault(data => 0 == string.Compare(data.item, msg, StringComparison.OrdinalIgnoreCase));

            return data?.idx ?? -1;
        }
    }
}
