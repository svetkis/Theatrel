using System;
using theatrel.Interfaces;

namespace theatrel.Lib
{
    public class PerformanceFilter : IPerformanceFilter
    {
        public DayOfWeek[] DaysOfWeek { get; set; }
        public string[] PerfomanceTypes { get; set; }
        public string[] Locations { get; set; }
    }
}
