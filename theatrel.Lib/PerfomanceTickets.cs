using System;
using System.Collections.Generic;
using System.Linq;
using theatrel.Interfaces;

namespace theatrel.Lib
{
    internal class PerfomanceTickets : IPerfomanceTickets
    {
        public string Description { get; set; }

        public DateTime LastUpdate { get; set; }

        public IDictionary<string, IDictionary<int, int>> Tickets { get; set; }
            = new Dictionary<string, IDictionary<int, int>>();

        public int GetMinPrice()
        {
            if (!Tickets.Any())
                return 0;

            return Tickets.Min(block => block.Value.Keys.Any() ? block.Value.Keys.Min() : 0);
        }
    }
}
