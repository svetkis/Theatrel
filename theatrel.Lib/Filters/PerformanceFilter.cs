using System;
using theatrel.Interfaces.Filters;

namespace theatrel.Lib.Filters
{
    internal class PerformanceFilter : IPerformanceFilter
    {
        public string PerformanceName { get; set; }

        public DayOfWeek[] DaysOfWeek { get; set; }
        public string[] PerformanceTypes { get; set; }
        public string[] Locations { get; set; }
        public int PartOfDay { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
