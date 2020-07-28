﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
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
            {7, DayOfWeek.Sunday }
        };

        private static readonly DayOfWeek[] Weekends = { DayOfWeek.Saturday, DayOfWeek.Sunday };
        private static readonly DayOfWeek[] AllDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        private static readonly DayOfWeek[] WeekDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        private static readonly string[] WeekendsNames = {"Выходные"};
        private static readonly string[] WeekDaysNames = { "Будни" };
        private static readonly string[] AllDaysNames = { "Любой", "не важно", "все"};

        private readonly ReplyKeyboardMarkup _daysOfWeekKeyboardMarkup;

        private readonly IDictionary<string, DayOfWeek[]> _daysDictionary = new Dictionary<string, DayOfWeek[]>();

        public DaysOfWeekCommand() : base((int)DialogStep.SelectDays)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");
            List<KeyboardButton> buttons = new List<KeyboardButton>();

            var daysArr = Enumerable.Range(1, 7).Select(idx =>
                new
                {
                    i = idx,
                    name = cultureRu.DateTimeFormat.GetDayName(_idxToDays[idx]).ToLower(),
                    abbrName = cultureRu.DateTimeFormat.GetAbbreviatedDayName(_idxToDays[idx]).ToLower()
                });

            foreach (var item in daysArr)
            {
                _daysDictionary.Add(item.i.ToString(), new[] { _idxToDays[item.i] });
                _daysDictionary.Add(item.name, new[] { _idxToDays[item.i] });
                _daysDictionary.Add(item.abbrName, new[] { _idxToDays[item.i] });

                buttons.Add(new KeyboardButton(item.name));
            }

            buttons.Add(new KeyboardButton(WeekDaysNames.First()));
            foreach (var name in WeekDaysNames)
                _daysDictionary.Add(name.ToLower(), WeekDays);

            buttons.Add(new KeyboardButton(WeekendsNames.First()));
            foreach (var name in WeekendsNames)
                _daysDictionary.Add(name.ToLower(), Weekends);

            buttons.Add(new KeyboardButton(AllDaysNames.First()));
            foreach (var name in AllDaysNames)
                _daysDictionary.Add(name.ToLower(), AllDays);

            _daysOfWeekKeyboardMarkup = new ReplyKeyboardMarkup
            {
                Keyboard = new[] { buttons.ToArray(), }
            };
        }

        public override async Task<ICommandResponse> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            var days = ParseMessage(message);
            chatInfo.Days = days;

            var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);
            return new TlCommandResponse(
                $"Вы выбрали {string.Join(" или ", chatInfo.Days.Select(d => culture.DateTimeFormat.GetDayName(d)))}. {ReturnMsg}");
        }

        public override bool IsMessageReturnToStart(string message)
        {
            var days = ParseMessage(message);
            return days.Any();
        }

        public override async Task<ICommandResponse> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("В какой день недели Вы хотели бы посетить театр? Вы можете выбрать несколько дней.");
            return new TlCommandResponse(stringBuilder.ToString(), _daysOfWeekKeyboardMarkup);
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