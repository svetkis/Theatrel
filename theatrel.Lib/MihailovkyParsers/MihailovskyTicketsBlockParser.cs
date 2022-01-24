using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.Interfaces.Helpers;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Tickets;

namespace theatrel.Lib.MihailovkyParsers;

internal class MihailovskyTicketsBlockParser : ITicketsParser
{
    private readonly IPageRequester _pageRequester;

    public MihailovskyTicketsBlockParser(IPageRequester pageRequester)
    {
        _pageRequester = pageRequester;
    }

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

        var content = await _pageRequester.RequestBytes(url, cancellationToken);
        return await PrivateParse(content, cancellationToken);
    }

    public async Task<IPerformanceTickets> Parse(byte[] data, CancellationToken cancellationToken)
        => await PrivateParse(data, cancellationToken);

    private async Task<IPerformanceTickets> PrivateParse(byte[] data, CancellationToken cancellationToken)
    {
        if (data == null || !data.Any())
            return new PerformanceTickets { State = TicketsState.TechnicalError };

        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream dataStream = new MemoryStream(data);
        using IDocument parsedDoc = await context.OpenAsync(req => req.Content(dataStream), cancellationToken);

        IPerformanceTickets performanceTickets = new PerformanceTickets { State = TicketsState.Ok };

        IElement specialInfo = parsedDoc.All
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

            ITicket ticketData = new Ticket { MinPrice = intPrice };
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

        performanceTickets.LastUpdate = DateTime.UtcNow;

        return performanceTickets;
    }
}