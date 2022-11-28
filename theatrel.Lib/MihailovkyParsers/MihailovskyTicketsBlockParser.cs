using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            return PrivateParse(content);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"ParseFromUrl error {url} {ex.Message}");
            throw;
        }
    }  

    private readonly byte[] _jsonTicketsBlockStart = Encoding.UTF8.GetBytes("JSON.parse('");
    private readonly byte[] _jsonTicketsBlockEnd = Encoding.UTF8.GetBytes("');");

    private IPerformanceTickets PrivateParse(byte[] data)
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
}