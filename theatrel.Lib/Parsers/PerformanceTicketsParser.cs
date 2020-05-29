using AngleSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;

namespace theatrel.Lib.Parsers
{
    public class PerformanceTicketsParser : ITicketsParser
    {
        private readonly ITicketParser _ticketParser = new TicketParser();

        public async Task<IPerfomanceTickets> ParseFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url) || url == CommonTags.NotDefined)
            {
                return new PerfomanceTickets()
                {
                    Description = CommonTags.NoTickets
                };
            }

            var content = await PageRequester.Request(url);
            return await PrivateParse(content);
        }

        public async Task<IPerfomanceTickets> Parse(string data) => await PrivateParse(data);

        private async Task<IPerfomanceTickets> PrivateParse(string data)
        {
            if (string.IsNullOrEmpty(data))
                return new PerfomanceTickets() { Description = CommonTags.RequestTicketsError };

            var context = BrowsingContext.New(Configuration.Default);
            var parsedDoc = await context.OpenAsync(req => req.Content(data));

            IPerfomanceTickets performanceTickets = new PerfomanceTickets();

            var tickets = parsedDoc.All.Where(m => 0 == string.Compare(m.TagName, "ticket", true));
            foreach(var ticket in tickets)
            {
                ITicket ticketData = _ticketParser.Parse(ticket);
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
                   performanceTickets.Tickets.Add(ticketData.Region, new Dictionary<int, int>() { { ticketData.MinPrice, 1 } });
                }
            }

            performanceTickets.LastUpdate = DateTime.Now;

            return performanceTickets;
        }
    }
}
