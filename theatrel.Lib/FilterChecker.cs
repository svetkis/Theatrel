using System.Linq;
using theatrel.Interfaces;

namespace theatrel.Lib
{
    public class FilterChecker : IFilterChecker
    {
        public bool IsDataSuitable(IPerformanceData performance, IPerformanceFilter filter)
        {
            if (filter.Locations != null && filter.Locations.Any() && !filter.Locations.Contains(performance.Location))
                return false;

            if (filter.PerformanceTypes != null && filter.PerformanceTypes.Any() 
                                                && filter.PerformanceTypes.All(val => 0 != string.Compare(val, performance.Type, true)))
                return false;

            if (filter.DaysOfWeek != null && filter.DaysOfWeek.Any() && !filter.DaysOfWeek.Contains(performance.DateTime.DayOfWeek))
                return false;

            return true;
        }
    }
}
