using System;
using System.Collections.Generic;

namespace theatrel.Lib
{
    public static class DateHelper
    {
        public static DateTime[] GetMonthsBetween(this DateTime from, DateTime to)
        {
            if (from > to)
                return new DateTime[0];

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
