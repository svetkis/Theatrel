using System;
using System.ComponentModel;

namespace theatrel.Interfaces.Filters;

[Flags]
public enum PartOfDay
{
    [Description("Before 12am")]
    Morning = 1,

    [Description("From 12am till 5pm")]
    Day = 2,

    [Description("From 5pm till 9pm")]
    Tonight = 4,

    [Description("From 9pm till 12pm")]
    Night = 8
}

public interface IPerformanceFilter
{
    string PerformanceName { get; set; }

    DayOfWeek[] DaysOfWeek { get; set; }
    string[] PerformanceTypes { get; set; }
    int[] TheatreIds { get; set; }
    int[] LocationIds { get; set; }
    int PartOfDay { get; set; }
    DateTime StartDate { get; set; }
    DateTime EndDate { get; set; }

    int PlaybillId { get; set; }
}