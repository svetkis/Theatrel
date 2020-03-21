using System;
using System.Linq;
using theatrel.Interfaces;

namespace theatrel.Lib
{
    public class PerformanceFilter : IPerformanceFilter
    {
        public DayOfWeek[] DaysOfWeek { get; set; }
        public string[] PerfomanceTypes { get; set; }
        public string[] Locations { get; set; }

        public bool Filter(IPerformanceData perfomance)
        {
            if (Locations != null && Locations.Any() && !Locations.Contains(perfomance.Location))
                return false;

            if (PerfomanceTypes != null && PerfomanceTypes.Any() && !PerfomanceTypes.Any(val => 0 == string.Compare(val, perfomance.Type, true)))
                return false;

            if (DaysOfWeek != null && DaysOfWeek.Any() && !DaysOfWeek.Contains(perfomance.DateTime.DayOfWeek))
                return false;

            if (perfomance.DateTime < DateTime.Now)
                return false;

            return true;
        }
    }
}
