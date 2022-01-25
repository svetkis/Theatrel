using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
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

        var content = await _pageRequester.RequestBytes(url, false, cancellationToken);
        return await PrivateParse(content, cancellationToken);
    }

    public async Task<IPerformanceTickets> Parse(byte[] data, CancellationToken cancellationToken)
        => await PrivateParse(data, cancellationToken);

    private async Task<IPerformanceTickets> PrivateParse(byte[] data, CancellationToken cancellationToken)
    {
        if (data == null || !data.Any())
            return new PerformanceTickets { State = TicketsState.TechnicalError };

        var ratesLabel = Encoding.UTF8.GetBytes("<p class=\"rates-label\">");
        var divCloseTag = Encoding.UTF8.GetBytes("</div>");
        int ratesLabelStartIndex = data.AsSpan().IndexOf(ratesLabel);
        int ratesEndIndex = ratesLabelStartIndex == -1 ? -1 :data.AsSpan(ratesLabelStartIndex).IndexOf(divCloseTag);

        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream dataStream = ratesLabelStartIndex > 0 && ratesEndIndex > 0
            ? new MemoryStream(data, ratesLabelStartIndex, ratesEndIndex + ratesLabelStartIndex + divCloseTag.Length)
            : new MemoryStream(data);

        using IDocument parsedDoc = await context.OpenAsync(req => req.Content(dataStream), cancellationToken);

        IPerformanceTickets performanceTickets = new PerformanceTickets { State = TicketsState.Ok };

        IElement specialInfo = ratesLabelStartIndex > 0
            ? null
            : parsedDoc.All.FirstOrDefault(m => 0 == string.Compare(m.ClassName, "rates-info desktop c-special-info", StringComparison.OrdinalIgnoreCase));

        IEnumerable<IElement> rates = ratesLabelStartIndex > 0
            ? parsedDoc.All.Where(m => 0 == string.Compare(m.ClassName, "rates-val", StringComparison.OrdinalIgnoreCase))
            : specialInfo?.Children[1].Children;

        if (rates == null)
            return new PerformanceTickets {State = TicketsState.Ok};

        foreach (var rate in rates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string price = new string(rate.TextContent.Where(char.IsDigit).ToArray());
            int.TryParse(price, out int intPrice);

            ITicket ticketData = new Ticket { MinPrice = intPrice, Region = "Зал" };

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