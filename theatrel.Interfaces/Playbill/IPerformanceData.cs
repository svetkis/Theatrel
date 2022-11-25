using System;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;

namespace theatrel.Interfaces.Playbill;

public interface IPerformanceData
{
    string Location { get; set; }
    string Name { get; set; }
    DateTime DateTime { get; set; }

    string Url { get; set; }
    string TicketsUrl { get; set; }

    TicketsState State { get; set; }

    string Type { get; set; }

    int MinPrice { get; set; }

    string CastFromPlaybill { get; set; }

    IPerformanceCast Cast { get; set; }

    string Description { get; set; }
}