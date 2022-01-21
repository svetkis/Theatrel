using System;
using System.Collections.Generic;
using theatrel.Common.Enums;

namespace theatrel.Interfaces.Tickets;

public interface IPerformanceTickets
{
    TicketsState State { get; set; }

    DateTime LastUpdate { get; set; }

    IDictionary<string, IDictionary<int, int>> Tickets { get; }

    int GetMinPrice();
}