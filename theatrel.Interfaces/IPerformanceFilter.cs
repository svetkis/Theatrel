using System;

namespace theatrel.Interfaces
{
    public interface IPerformanceFilter : IDIRegistrable
    {
        DayOfWeek[] DaysOfWeek { get; set; }
        string[] PerformanceTypes { get; set; }
        string[] Locations { get; set; }
    }
}
