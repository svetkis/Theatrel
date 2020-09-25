using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace theatrel.Common.FormatHelper
{
    public class DaysOfWeekHelper
    {
        public static readonly DayOfWeek[] Weekends = { DayOfWeek.Saturday, DayOfWeek.Sunday };
        public static readonly DayOfWeek[] AllDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        public static readonly DayOfWeek[] WeekDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        public static readonly string[] WeekendsNames = { "Выходные" };
        public static readonly string[] WeekDaysNames = { "Будни" };
        public static readonly string[] AllDaysNames = { "Любой", "не важно", "все" };

        public static readonly IDictionary<string, DayOfWeek[]> DaysDictionary = new Dictionary<string, DayOfWeek[]>();

        static DaysOfWeekHelper()
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");

            var daysArr = Enumerable.Range(1, 7).Select(idx =>
                new
                {
                    i = idx,
                    name = cultureRu.DateTimeFormat.GetDayName((DayOfWeek)(idx % 7)).ToLower(),
                    abbrName = cultureRu.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)(idx % 7)).ToLower()
                });

            DaysDictionary[WeekendsNames.First()] = Weekends;
            DaysDictionary[WeekDaysNames.First()] = WeekDays;
            DaysDictionary[AllDaysNames.First()] = AllDays;

            foreach (var item in daysArr)
            {
                int idx = item.i % 7;
                DaysDictionary.Add(item.i.ToString(), new[] { (DayOfWeek)idx });
                DaysDictionary.Add(item.name, new[] { (DayOfWeek)idx });
                DaysDictionary.Add(item.abbrName, new[] { (DayOfWeek)idx });
            }
        }

        public static string GetDaysDescription(DayOfWeek[] days, CultureInfo culture)
        {
            if (days == null)
                return AllDaysNames.First().ToLower();

            if (days.Length > 1)
            {
                var sorted = days.OrderBy(d => (int)d, DaysOfWeekComparer.Create()).ToArray();
                foreach (var dict in DaysDictionary)
                {
                    if (dict.Value.SequenceEqual(sorted))
                        return dict.Key;
                }
            }

            IEnumerable<string> daysArr = days
                .OrderBy(d => (int)d, DaysOfWeekComparer.Create())
                .Select(d => culture.DateTimeFormat.GetDayName(d));

            return string.Join(" или ", daysArr);
        }
    }
}
