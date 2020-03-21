using System;

namespace theatrel.Interfaces
{
    public interface IPerformanceFilter : IDIRegistrableService
    {
        DayOfWeek[] DaysOfWeek { get; set; }
        string[] PerfomanceTypes { get; set; }
        string[] Locations { get; set; }

        bool Filter(IPerformanceData perfomance);
    }
}
