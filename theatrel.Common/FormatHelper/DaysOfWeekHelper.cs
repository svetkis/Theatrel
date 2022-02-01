using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace theatrel.Common.FormatHelper;

public static class DaysOfWeekHelper
{
    public static readonly DayOfWeek[] Weekends = { DayOfWeek.Saturday, DayOfWeek.Sunday };
    public static readonly DayOfWeek[] AllDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
    public static readonly DayOfWeek[] WeekDays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    public static readonly string[] WeekendsNames = { "Выходные" };
    public static readonly string[] WeekDaysNames = { "Будни" };
    public static readonly string[] AllDaysNames = { "Любой", "не важно", "все" };

    public static readonly IReadOnlyDictionary<string, DayOfWeek[]> DaysDictionary = GetDaysDictionary(CultureInfo.CreateSpecificCulture("ru"));

    private static readonly IComparer<int> IntComparer = DaysOfWeekComparer.Create();

    private static IReadOnlyDictionary<string, DayOfWeek[]> GetDaysDictionary(CultureInfo culture)
    {
        var daysDictionary = new Dictionary<string, DayOfWeek[]>
        {
            [WeekendsNames.First()] = Weekends,
            [WeekDaysNames.First()] = WeekDays,
            [AllDaysNames.First()] = AllDays
        };

        foreach (var index in Enumerable.Range(1, 7))
        {
            int dayOfWeekIndex = index % 7;
            DayOfWeek dayOfWeek = (DayOfWeek)dayOfWeekIndex;

            var name = culture.DateTimeFormat.GetDayName(dayOfWeek).ToLower();
            var abbrName = culture.DateTimeFormat.GetAbbreviatedDayName(dayOfWeek).ToLower();
            var dayArray = new[] { dayOfWeek };

            daysDictionary.Add(index.ToString(), dayArray);
            daysDictionary.Add(name, dayArray);
            daysDictionary.Add(abbrName, dayArray);
        }

        return daysDictionary;
    }

    public static string GetDaysDescription(IEnumerable<DayOfWeek> days, CultureInfo culture)
    {
        var daysArray = days?.ToArray();
        if (daysArray == null || !daysArray.Any())
            return AllDaysNames.First().ToLower();

        if (daysArray.Count() > 1)
        {
            var sorted = daysArray.OrderBy(d => (int)d, IntComparer).ToArray();
            foreach (var (key, _) in DaysDictionary.Where(keyValuePair => keyValuePair.Value.SequenceEqual(sorted)))
            {
                return key;
            }
        }

        IEnumerable<string> returnArray = daysArray
            .OrderBy(d => (int)d, IntComparer)
            .Select(d => culture.DateTimeFormat.GetDayName(d));

        return string.Join(" или ", returnArray);
    }
}