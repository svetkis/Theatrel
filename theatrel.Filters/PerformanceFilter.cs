﻿using theatrel.Interfaces.Filters;

namespace theatrel.Lib.Filters;

internal class PerformanceFilter : IPerformanceFilter
{
    public int PlaybillId { get; set; } = -1;
    public string PerformanceName { get; set; }
    public string Actor { get; set; }

    public DayOfWeek[] DaysOfWeek { get; set; }
    public string[] PerformanceTypes { get; set; }
    public int[] TheatreIds { get; set; }
    public int[] LocationIds { get; set; }
    public int PartOfDay { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }   
}