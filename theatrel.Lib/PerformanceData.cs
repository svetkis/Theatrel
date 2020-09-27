using System;
using System.Net;
using theatrel.Common.Enums;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib
{
    internal class PerformanceData : IPerformanceData
    {
        public string Location { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public string Url { get; set; }
        public string TicketsUrl { get; set; }
        public TicketsState State { get; set; }

        public string Type { get; set; }
        public int MinPrice { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
