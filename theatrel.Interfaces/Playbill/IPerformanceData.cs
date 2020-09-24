using System;

namespace theatrel.Interfaces.Playbill
{
    public interface IPerformanceData
    {
        string Location { get; set; }
        string Name { get; set; }
        DateTime DateTime { get; set; }

        string Url { get; set; }
        string TicketsUrl { get; set; }

        string Type { get; set; }

        int MinPrice { get; set; }
    }
}
