using System;
using System.Linq;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib.Filters
{
    internal class FilterChecker : IFilterChecker
    {
        public bool IsDataSuitable(IPerformanceData performance, IPerformanceFilter filter)
        {
            if (filter == null)
                return true;

            if (filter.Locations != null && filter.Locations.Any() && !filter.Locations.Contains(performance.Location))
                return false;

            if (filter.PerformanceTypes != null && filter.PerformanceTypes.Any()
                                                && filter.PerformanceTypes.All(val => 0 != string.Compare(val, performance.Type, true)))
                return false;

            if (filter.DaysOfWeek != null && filter.DaysOfWeek.Any() && !filter.DaysOfWeek.Contains(performance.DateTime.DayOfWeek))
                return false;

            return true;
        }

        public bool IsDataSuitable(string location, string type, DateTime when, IPerformanceFilter filter)
        {
            if (filter == null)
                return true;

            if (filter.Locations != null && filter.Locations.Any() && !filter.Locations.Contains(location))
                return false;

            if (filter.PerformanceTypes != null && filter.PerformanceTypes.Any()
                                                && filter.PerformanceTypes.All(val => 0 != string.Compare(val, type, true)))
                return false;

            if (filter.DaysOfWeek != null && filter.DaysOfWeek.Any() && !filter.DaysOfWeek.Contains(when.DayOfWeek))
                return false;

            return true;
        }
    }
}
