using System;
using System.Collections.Generic;
using System.Linq;
using theatrel.Common.Enums;
using theatrel.Interfaces.Tickets;

namespace theatrel.Lib.Tickets;

internal class PerformanceTickets : IPerformanceTickets
{
    public TicketsState State { get; set; }
    public DateTime LastUpdate { get; set; }

    public IDictionary<string, IDictionary<int, int>> Tickets { get; set; }
        = new Dictionary<string, IDictionary<int, int>>();

    public int GetMinPrice()
    {
        return !Tickets.Any()
            ? 0
            : Tickets.Min(block => block.Value.Keys.Any() ? block.Value.Keys.Min() : 0);
    }
}