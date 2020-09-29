using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.Interfaces.Tickets;

namespace theatrel.Lib.Tickets
{
    internal class PerformanceTicketsParser : ITicketsParser
    {
        private readonly ITicketParser _ticketParser = new TicketParser();

        public async Task<IPerformanceTickets> ParseFromUrl(string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
                return new PerformanceTickets { State = TicketsState.NoTickets };

            switch (url)
            {
                case CommonTags.NotDefinedTag:
                    return new PerformanceTickets { State = TicketsState.NoTickets };
                case CommonTags.NoTicketsTag:
                    return new PerformanceTickets { State = TicketsState.NoTickets };
                case CommonTags.WasMovedTag:
                    return new PerformanceTickets { State = TicketsState.PerformanceWasMoved };
            }

            var content = await PageRequester.Request(url, cancellationToken);
            return await PrivateParse(content, cancellationToken);
        }

        public async Task<IPerformanceTickets> Parse(string data, CancellationToken cancellationToken)
            => await PrivateParse(data, cancellationToken);

        private async Task<IPerformanceTickets> PrivateParse(string data, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(data))
                return new PerformanceTickets { State = TicketsState.TechnicalError };

            var context = BrowsingContext.New(Configuration.Default);
            var parsedDoc = await context.OpenAsync(req => req.Content(data), cancellationToken);

            IPerformanceTickets performanceTickets = new PerformanceTickets() {State = TicketsState.Ok};

            var tickets = parsedDoc.All.Where(m => 0 == string.Compare(m.TagName, "ticket", StringComparison.OrdinalIgnoreCase));
            foreach (var ticket in tickets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ITicket ticketData = _ticketParser.Parse(ticket, cancellationToken);
                if (string.IsNullOrEmpty(ticketData.Region))
                    ticketData.Region = "Зал";

                if (performanceTickets.Tickets.ContainsKey(ticketData.Region))
                {
                    IDictionary<int, int> block = performanceTickets.Tickets[ticketData.Region];
                    if (block.ContainsKey(ticketData.MinPrice))
                        ++block[ticketData.MinPrice];
                    else
                        block.Add(ticketData.MinPrice, 1);
                }
                else
                {
                    performanceTickets.Tickets.Add(ticketData.Region, new Dictionary<int, int> { { ticketData.MinPrice, 1 } });
                }
            }

            if (!tickets.Any())
                performanceTickets.State = TicketsState.TechnicalError;

            performanceTickets.LastUpdate = DateTime.Now;

            return performanceTickets;
        }
    }
}
