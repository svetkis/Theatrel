using System;
using System.Linq;
using theatrel.Interfaces;

namespace theatrel.Lib
{
    public class FilterChecker : IFilterChecker
    {
        public bool IsDataSuitable(IPerformanceData perfomance, IPerformanceFilter filter)
        {
            if (filter.Locations != null && filter.Locations.Any() && !filter.Locations.Contains(perfomance.Location))
                return false;

            if (filter.PerfomanceTypes != null && filter.PerfomanceTypes.Any() && !filter.PerfomanceTypes.Any(val => 0 == string.Compare(val, perfomance.Type, true)))
                return false;

            if (filter.DaysOfWeek != null && filter.DaysOfWeek.Any() && !filter.DaysOfWeek.Contains(perfomance.DateTime.DayOfWeek))
                return false;

            return true;
        }
    }
}
