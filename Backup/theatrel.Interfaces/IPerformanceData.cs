using System;
using System.Net;

namespace theatrel.Interfaces
{
    public interface IPerformanceData : IDIRegistrable
    {
        string Location { get; set; }
        string Name { get; set; }
        DateTime DateTime { get; set; }
        string Url { get; set; }
        string Type { get; set; }
        IPerfomanceTickets Tickets { get; set; }
        HttpStatusCode StatusCode { get; set; }
    }
}
