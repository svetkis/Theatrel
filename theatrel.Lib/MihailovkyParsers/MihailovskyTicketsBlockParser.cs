using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.MariinskyParsers;
using theatrel.Lib.Tickets;

namespace theatrel.Lib.MihailovkyParsers
{
    internal class MihailovskyTicketsBlockParser : ITicketsParser
    {
        private readonly ITicketParser _ticketParser = new MariinskyTicketParser();

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

            IPerformanceTickets performanceTickets = new PerformanceTickets { State = TicketsState.Ok };

            var specialInfo = parsedDoc.All
                .FirstOrDefault(m => 0 == string.Compare(m.ClassName, "rates-info desktop c-special-info",
                    StringComparison.OrdinalIgnoreCase));

            IHtmlCollection<IElement> rates = specialInfo?.Children[1].Children;
            if (rates == null)
                return new PerformanceTickets {State = TicketsState.Ok};

            foreach (var rate in rates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string price = rate.TextContent.Replace("руб", "").Trim().Replace(" ", "").Replace(".", "");
                int.TryParse(price, out int intPrice);

                ITicket ticketData = new Ticket{MinPrice = intPrice };
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

            if (!performanceTickets.Tickets.Any())
                performanceTickets.State = TicketsState.TechnicalError;

            performanceTickets.LastUpdate = DateTime.Now;

            return performanceTickets;
        }
    }
}
