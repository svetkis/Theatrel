using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class DaysOfWeekCommand : DialogCommandBase
    {
        private readonly IDictionary<int, DayOfWeek> _idxToDays = new Dictionary<int, DayOfWeek>
        {
            {1, DayOfWeek.Monday },
            {2, DayOfWeek.Tuesday },
            {3, DayOfWeek.Wednesday },
            {4, DayOfWeek.Thursday },
            {5, DayOfWeek.Friday },
            {6, DayOfWeek.Saturday },
            {7, DayOfWeek.Sunday },
        };

        private readonly IDictionary<string, DayOfWeek[]> _daysDictionary = new Dictionary<string, DayOfWeek[]>
        {
            {"1", new [] { DayOfWeek.Monday}},
            {"2", new [] { DayOfWeek.Tuesday } },
            {"3", new [] { DayOfWeek.Wednesday } },
            {"4", new [] { DayOfWeek.Thursday } },
            {"5", new [] { DayOfWeek.Friday } },
            {"6", new [] { DayOfWeek.Saturday } },
            {"7", new [] { DayOfWeek.Sunday } },
            {"будни", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } },
            {"выходные", new [] { DayOfWeek.Saturday, DayOfWeek.Sunday } },
            {"все", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday } },
            {"любой", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday } },
        };

        public DaysOfWeekCommand() : base((int)DialogStep.SelectDays)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");
            foreach(var item in Enumerable.Range(1, 7).Select(idx => new { i = idx, name = cultureRu.DateTimeFormat.GetDayName(_idxToDays[idx]).ToLower() }))
                _daysDictionary.Add(item.name, new[] { _idxToDays[item.i] });

            foreach (var item in Enumerable.Range(1, 7).Select(idx => new { i = idx, name = cultureRu.DateTimeFormat.GetAbbreviatedDayName(_idxToDays[idx]).ToLower() }))
                _daysDictionary.Add(item.name, new[] { _idxToDays[item.i] });
        }

        public override string ApplyResult(IChatDataInfo chatInfo, string message)
        {
            var days = ParseMessage(message);
            chatInfo.Days = days;

            var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);
            return $"Вы выбрали {string.Join(" или ", chatInfo.Days.Select(d => culture.DateTimeFormat.GetDayName(d)))}. Для того что бы выбрать другое напишите Нет.";
        }

        public override bool IsMessageClear(string message)
        {
            var days = ParseMessage(message);
            return days.Any();
        }

        public override async Task<string> ExecuteAsync(IChatDataInfo chatInfo)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("В какой день недели Вы хотели бы посетить театр? Вы можете выбрать несколько дней.");
            return stringBuilder.ToString();
        }

        private DayOfWeek[] ParseMessage(string message)
        {
            List<DayOfWeek> days = new List<DayOfWeek>();
            foreach(var daysArray in SplitMessage(message).Select(ParseMessagePart).Where(arr => arr != null && arr.Any()))
            {
                days.AddRange(daysArray);
            }

            return days.ToArray();
        }

        private string[] SplitMessage(string message) => message.Split(WordSplitters).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        private DayOfWeek[] ParseMessagePart(string messagePart)
        {
            if (string.IsNullOrEmpty(messagePart))
                return new DayOfWeek[0];

            string key = messagePart.ToLower();

            if (!_daysDictionary.ContainsKey(key))
                return new DayOfWeek[0];

            return _daysDictionary[key];
        }
    }
}