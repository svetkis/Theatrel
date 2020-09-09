using System;
using System.Linq;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using theatrel.Interfaces.TgBot;

namespace theatrel.Lib.Filters
{
    internal class FilterService : IFilterService
    {
        public IPerformanceFilter GetFilter(IChatDataInfo dataInfo)
        {
            var filter = new PerformanceFilter
            {
                StartDate = dataInfo.When,
                EndDate = dataInfo.When.AddMonths(1).AddDays(-1)
            };

            if (dataInfo.Days != null && dataInfo.Days.Any())
            {
                var days = dataInfo.Days.Distinct().ToArray();
                if (days.Count() < 7)
                    filter.DaysOfWeek = days.ToArray();
            }

            if (dataInfo.Types != null && dataInfo.Types.Any())
                filter.PerformanceTypes = dataInfo.Types;

            return filter;
        }

        public IPerformanceFilter GetFilter(DateTime start, DateTime end) =>
            new PerformanceFilter
            {
                StartDate = start,
                EndDate = end
            };

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

            if (filter.Locations != null && filter.Locations.Any() && !string.IsNullOrEmpty(filter.Locations.First())
                && !filter.Locations.Contains(location))
                return false;

            if (filter.PerformanceTypes != null && filter.PerformanceTypes.Any() && !string.IsNullOrEmpty(filter.PerformanceTypes.First())
                                                && filter.PerformanceTypes.All(val => 0 != string.Compare(val, type, true)))
                return false;

            if (filter.DaysOfWeek != null && filter.DaysOfWeek.Any() && !filter.DaysOfWeek.Contains(when.DayOfWeek))
                return false;

            if (filter.StartDate > when || filter.EndDate < when)
                return false;

            return true;
        }
    }
}