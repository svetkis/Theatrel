using System;
using System.Net;
using theatrel.Interfaces;

namespace theatrel.Lib
{
    public class PerformanceData : IPerformanceData
    {
        public string Location { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public IPerfomanceTickets Tickets { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
