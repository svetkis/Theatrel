using System;
using System.Collections.Generic;

namespace theatrel.Interfaces
{
    public interface IPerfomanceTickets
    {
        string Description { get; set; }

        DateTime LastUpdate { get; set; }

        IDictionary<string, IDictionary<int, int>> Tickets { get; }

        int GetMinPrice();
    }
}
