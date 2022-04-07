using System;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib;

internal class PerformanceData : IPerformanceData
{
    public int TheatreId { get; set; }
    public string TheatreName { get; set; }

    public string Location { get; set; }
    public string Name { get; set; }
    public DateTime DateTime { get; set; }
    public string Url { get; set; }
    public string TicketsUrl { get; set; }
    public TicketsState State { get; set; }

    public string Type { get; set; }
    public int MinPrice { get; set; }
    public IPerformanceCast Cast { get; set; }
    public string Description { get; set; }
}