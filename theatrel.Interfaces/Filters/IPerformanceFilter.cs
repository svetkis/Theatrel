using System;

namespace theatrel.Interfaces.Filters;

public interface IPerformanceFilter
{
    string PerformanceName { get; set; }

    string Actor { get; set; }

    DayOfWeek[] DaysOfWeek { get; set; }

    string[] PerformanceTypes { get; set; }

    int[] TheatreIds { get; set; }

    int[] LocationIds { get; set; }

    DateTime StartDate { get; set; }

    DateTime EndDate { get; set; }

    int PlaybillId { get; set; }
}