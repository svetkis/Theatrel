using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Newtonsoft.Json;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.Interfaces.EncodingService;
using theatrel.Interfaces.Helpers;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Tickets;

namespace theatrel.Lib.MihailovkyParsers;

internal class MihailovskyTicketsBlockParser : ITicketsParser
{
    private readonly IPageRequester _pageRequester;
    private readonly IEncodingService _encodingService;

    public MihailovskyTicketsBlockParser(IPageRequester pageRequester, IEncodingService encodingService)
    {
        _pageRequester = pageRequester;
        _encodingService = encodingService;
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

        try
        {
            return PrivateParse2(content, cancellationToken);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"ParseFromUrl error {url} {ex.Message}");
            throw;
        }
    }

    public async Task<IPerformanceTickets> Parse(byte[] data, CancellationToken cancellationToken)
    {
        return await PrivateParse(data, cancellationToken);
    }   

    private readonly byte[] _ratesLabel = Encoding.UTF8.GetBytes("<p class=\"rates-label\">");
    private readonly byte[] _divCloseTag = Encoding.UTF8.GetBytes("</div>");
    private readonly byte[] _jsonTicketsBlockStart = Encoding.UTF8.GetBytes("JSON.parse('");
    private readonly byte[] _jsonTicketsBlockEnd = Encoding.UTF8.GetBytes("');");

    private IPerformanceTickets PrivateParse2(byte[] data, CancellationToken cancellationToken)
    {
        if (data == null || !data.Any())
            return new PerformanceTickets { State = TicketsState.TechnicalError };

        int jsonTicketsBlockStartIndex = data.AsSpan().IndexOf(_jsonTicketsBlockStart);

        if (jsonTicketsBlockStartIndex == -1)
            return new PerformanceTickets { State = TicketsState.TechnicalError };

        int jsonTicketsBlockEndIndex = data.AsSpan(jsonTicketsBlockStartIndex).IndexOf(_jsonTicketsBlockEnd);

        if (jsonTicketsBlockEndIndex == -1)
            return new PerformanceTickets { State = TicketsState.TechnicalError };

        var encoding1251 = _encodingService.Get1251Encoding();

        string json = encoding1251.GetString(
            data.AsSpan(
                jsonTicketsBlockStartIndex + _jsonTicketsBlockStart.Length,
                jsonTicketsBlockEndIndex - _jsonTicketsBlockStart.Length));

        var seats = JsonConvert.DeserializeObject<Dictionary<long, Seat>>(json);

        var validSeats = seats.Select(x => x.Value).Where(x =>
        {
            if (x.IS_BUSY)
                return false;

            if (x.PROPERTY_NOTE_EN_VALUE == null)
                return true;

            return !x.PROPERTY_NOTE_EN_VALUE.ToString().Contains("wheelchair");
        });

        if (!validSeats.Any())
        {
            return new PerformanceTickets { State = TicketsState.Ok };
        }

        var minPrice = validSeats.Select(x => x.PRICE).Min(x => x);

        return new PerformanceTickets
        {
            State = TicketsState.Ok,
            LastUpdate = DateTime.UtcNow,
            MinTicketPrice = minPrice
        };
    }

    private async Task<IPerformanceTickets> PrivateParse(byte[] data, CancellationToken cancellationToken)
    {
        if (data == null || !data.Any())
            return new PerformanceTickets { State = TicketsState.TechnicalError };

        int ratesLabelStartIndex = data.AsSpan().IndexOf(_ratesLabel);
        int ratesEndIndex = ratesLabelStartIndex == -1
            ? -1
            : data.AsSpan(ratesLabelStartIndex).IndexOf(_divCloseTag);

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