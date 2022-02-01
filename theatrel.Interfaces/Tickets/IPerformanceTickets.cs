using System;
using theatrel.Common.Enums;

namespace theatrel.Interfaces.Tickets;

public interface IPerformanceTickets
{
    TicketsState State { get; set; }

    DateTime LastUpdate { get; set; }

    int MinTicketPrice { get; set; }
}