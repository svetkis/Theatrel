using System;

namespace theatrel.Interfaces
{
    public interface IPerformanceFilter : IDIRegistrable
    {
        DayOfWeek[] DaysOfWeek { get; set; }
        string[] PerfomanceTypes { get; set; }
        string[] Locations { get; set; }
    }
}
