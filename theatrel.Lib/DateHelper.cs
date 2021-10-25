using System;
using System.Collections.Generic;

namespace theatrel.Lib
{
    public static class DateHelper
    {
        public static DateTime[] GetMonthsBetween(this DateTime from, DateTime to)
        {
            if (from > to)
                return Array.Empty<DateTime>();

            if (from.Year == to.Year && from.Month == to.Month)
                return new[] { from };

            to = to.AddSeconds(-1); // for skipping next month if date is only first day of month 00:00:00

            var monthDiff = Math.Abs(to.Year * 12 + (to.Month - 1) - (from.Year * 12 + (from.Month - 1)));

            List<DateTime> results = new List<DateTime>();
            for (int i = monthDiff; i >= 0; --i)
            {
                results.Add(to.AddMonths(-i));
            }

            return results.ToArray();
        }
    }
}
