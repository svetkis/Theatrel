using System;
using theatrel.Interfaces;

namespace theatrel.Lib
{
    public class PerformanceFilter : IPerformanceFilter
    {
        public DayOfWeek[] DaysOfWeek { get; set; }
        public string[] PerformanceTypes { get; set; }
        public string[] Locations { get; set; }
        public int PartOfDay { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
