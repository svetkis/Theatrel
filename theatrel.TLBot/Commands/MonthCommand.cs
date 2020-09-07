using System;
using System.Globalization;
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
    internal class MonthCommand : DialogCommandBase
    {
        private string GoodDay = "Добрый день! ";
        private string IWillHelpYou = "Я помогу вам подобрать билеты в Мариинский театр. ";
        private string Msg = "Какой месяц вас интересует?";

        private readonly string[] _monthNames;
        private readonly string[] _monthNamesAbbreviated;

        protected override string ReturnCommandMessage { get; set; } = "Выбрать другой месяц";

        public override string Name => "Выбрать месяц";

        public MonthCommand(IDbService dbService) : base((int)DialogStep.SelectMonth, dbService)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");

            _monthNames = Enumerable.Range(1, 12).Select(num => cultureRu.DateTimeFormat.GetMonthName(num)).ToArray();
            _monthNamesAbbreviated = Enumerable.Range(1, 12).Select(num => cultureRu.DateTimeFormat.GetAbbreviatedMonthName(num)).ToArray();

            var buttons = _monthNames.Select(m => new KeyboardButton(m)).ToArray();
            CommandKeyboardMarkup = new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(buttons, ButtonsInLine),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }

        public override bool IsMessageCorrect(string message) => 0 != GetMonth(message.Trim().ToLower());

        private int GetMonth(string msg)
        {
            if (int.TryParse(msg, out var value))
            {
                if (value > 0 && value < 12)
                    return value;

                return 0;
            }

            int num = CheckEnumerable(_monthNames, msg);
            if (num != 0)
                return num;

            int numAbr = CheckEnumerable(_monthNamesAbbreviated, msg);

            return numAbr;
        }

        public override Task<ITgOutboundMessage> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            int month = GetMonth(message.Trim().ToLower());

            int year = DateTime.Now.Month > month ? DateTime.Now.Year + 1 : DateTime.Now.Year;

            chatInfo.When = new DateTime(year, month, 1);

            var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);
            return Task.FromResult<ITgOutboundMessage>(new TgOutboundMessage(
                $"Вы выбрали {culture.DateTimeFormat.GetMonthName(month)} {year}. {ReturnMsg}", ReturnKeyboardMarkup));
        }

        public override Task<ITgOutboundMessage> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            switch (chatInfo.DialogState)
            {
                case DialogStateEnum.DialogReturned:
                    return Task.FromResult<ITgOutboundMessage>(new TgOutboundMessage(Msg, CommandKeyboardMarkup));
                case DialogStateEnum.DialogStarted:
                    return Task.FromResult<ITgOutboundMessage>(new TgOutboundMessage($"{GoodDay}{IWillHelpYou}{Msg}", CommandKeyboardMarkup));
                default:
                    throw new NotImplementedException();
            }
        }

        private int CheckEnumerable(string[] checkedData, string msg)
        {
            var monthData = checkedData.Select((month, idx) => new { idx, month })
                .FirstOrDefault(data => msg.Equals(data.month, StringComparison.OrdinalIgnoreCase));

            return monthData?.idx + 1 ?? 0;
        }
    }
}
