using System;
using System.Collections.Generic;
using theatrel.Common.Enums;
using theatrel.Interfaces.Tickets;

namespace theatrel.Lib.Tickets;

internal class PerformanceTickets : IPerformanceTickets
{
    public TicketsState State { get; set; }
    public DateTime LastUpdate { get; set; }

    public IDictionary<string, IDictionary<int, int>> Tickets { get; set; }
        = new Dictionary<string, IDictionary<int, int>>();

    public int MinTicketPrice { get; set; }
}