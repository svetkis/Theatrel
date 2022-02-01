using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

    private readonly byte[] _ratesLabel = Encoding.UTF8.GetBytes("<p class=\"rates-label\">");
    private readonly byte[] _divCloseTag = Encoding.UTF8.GetBytes("</div>");

    private async Task<IPerformanceTickets> PrivateParse(byte[] data, CancellationToken cancellationToken)
    {
        if (data == null || !data.Any())
            return new PerformanceTickets { State = TicketsState.TechnicalError };


        int ratesLabelStartIndex = data.AsSpan().IndexOf(_ratesLabel);
        int ratesEndIndex = ratesLabelStartIndex == -1 ? -1 : data.AsSpan(ratesLabelStartIndex).IndexOf(_divCloseTag);

        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream dataStream = ratesLabelStartIndex > 0 && ratesEndIndex > 0
            ? new MemoryStream(data, ratesLabelStartIndex, ratesEndIndex + ratesLabelStartIndex + _divCloseTag.Length)
            : new MemoryStream(data);

        using IDocument parsedDoc = await context.OpenAsync(req => req.Content(dataStream), cancellationToken);

        IPerformanceTickets performanceTickets = new PerformanceTickets { State = TicketsState.Ok };

        IElement specialInfo = ratesLabelStartIndex > 0 ? null : parsedDoc.QuerySelector("div.rates-info");

        IEnumerable<IElement> rates = ratesLabelStartIndex > 0
            ? parsedDoc.QuerySelectorAll("span.rates-val")
            : specialInfo?.Children[1].Children;

        if (rates == null)
            return new PerformanceTickets {State = TicketsState.Ok};

        var prices = rates
            .Select(rate =>
            {
                int.TryParse(new string(rate.TextContent.Where(char.IsDigit).ToArray()), out int parsedPrice);
                return parsedPrice;
            })
            .Where(p => p > 0)
            .ToArray();

        performanceTickets.MinTicketPrice = prices.Any() ? prices.Min() : 0;

        if (performanceTickets.MinTicketPrice < 1)
            performanceTickets.State = TicketsState.TechnicalError;

        performanceTickets.LastUpdate = DateTime.UtcNow;

        return performanceTickets;
    }
}