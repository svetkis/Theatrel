using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common.FormatHelper;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchByDate
{
    internal class DaysOfWeekCommand : DialogCommandBase
    {
        protected override string ReturnCommandMessage { get; set; } = "Выбрать другие дни";

        public override string Name => "Выберите день недели";

        public DaysOfWeekCommand(IDbService dbService) : base(dbService)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");
            List<KeyboardButton> buttons = new List<KeyboardButton>
            {
                new KeyboardButton(DaysOfWeekHelper.WeekDaysNames.First()),
                new KeyboardButton(DaysOfWeekHelper.WeekendsNames.First()),
                new KeyboardButton(DaysOfWeekHelper.AllDaysNames.First())
            };

            foreach (var idx in Enumerable.Range(1, 7))
            {
                buttons.Add(new KeyboardButton(cultureRu.DateTimeFormat.GetDayName((DayOfWeek)(idx % 7)).ToLower()));
            }

            CommandKeyboardMarkup = new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(ButtonsInLine, buttons),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }

        private const string YouSelected = "Вы выбрали";
        public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            var days = ParseMessage(message);
            chatInfo.Days = days;

            var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);
            string responseDays = DaysOfWeekHelper.GetDaysDescription(chatInfo.Days, culture);

            return Task.FromResult<ITgCommandResponse>(
               new TgCommandResponse($"{YouSelected} {responseDays}. {ReturnMsg}", ReturnCommandMessage));
        }

        public override bool IsMessageCorrect(string message)
        {
            DayOfWeek[] days = ParseMessage(message);
            return days.Any();
        }

        public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("В какой день недели Вы хотели бы посетить театр? Вы можете выбрать несколько дней.");
            return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(stringBuilder.ToString(), CommandKeyboardMarkup));
        }

        private DayOfWeek[] ParseMessage(string message)
        {
            List<DayOfWeek> days = new List<DayOfWeek>();
            foreach (var daysArray in SplitMessage(message).Select(ParseMessagePart).Where(arr => arr != null && arr.Any()))
            {
                days.AddRange(daysArray);
            }

            return days.Distinct().OrderBy(item => (int)item).ToArray();
        }

        private string[] SplitMessage(string message) => message.Split(WordSplitters).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        private DayOfWeek[] ParseMessagePart(string messagePart)
        {
            if (string.IsNullOrEmpty(messagePart))
                return new DayOfWeek[0];

            if (DaysOfWeekHelper.WeekendsNames.Any(name => string.Equals(name, messagePart, StringComparison.OrdinalIgnoreCase)))
                return DaysOfWeekHelper.Weekends;

            if (DaysOfWeekHelper.WeekDaysNames.Any(name => string.Equals(name, messagePart, StringComparison.OrdinalIgnoreCase)))
                return DaysOfWeekHelper.WeekDays;

            if (DaysOfWeekHelper.AllDaysNames.Any(name => string.Equals(name, messagePart, StringComparison.OrdinalIgnoreCase)))
                return DaysOfWeekHelper.AllDays;

            string key = messagePart.ToLower();

            return !DaysOfWeekHelper.DaysDictionary.ContainsKey(key) ? new DayOfWeek[0] : DaysOfWeekHelper.DaysDictionary[key];
        }
    }
}