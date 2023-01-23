using System;
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
using theatrel.Lib.Utils;

namespace theatrel.Lib.MariinskyParsers;

internal class MariinskyTicketsBlockParser : ITicketsParser
{
    private readonly IPageRequester _pageRequester;

    public MariinskyTicketsBlockParser(IPageRequester pageRequester)
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

        if (url.Contains("kassir.ru", StringComparison.InvariantCultureIgnoreCase))
            return await PrivateParseKassir(content, cancellationToken);

        return PrivateParse(content);
    }

    private static string priceTag = "<tprice";
    private static string closePriceTag = "</tprice";

    private readonly byte[] _priceTagBytes = Encoding.UTF8.GetBytes(priceTag);
    private readonly int _priceTagBytesLength = priceTag.Length;

    private readonly byte[] _closePriceTagBytes = Encoding.UTF8.GetBytes(closePriceTag);
    private readonly int _closePriceTagBytesLength = closePriceTag.Length;

    private readonly byte _closeTagBytes = Encoding.UTF8.GetBytes(">").First();

    private IPerformanceTickets PrivateParse(byte[] data)
    {
        int priceTagIndex = data.AsSpan().IndexOf(_priceTagBytes);
        if (-1 == priceTagIndex)
        {
            return new PerformanceTickets { State = TicketsState.TechnicalError, LastUpdate = DateTime.UtcNow };
        }

        IPerformanceTickets performanceTickets = new PerformanceTickets { State = TicketsState.Ok };

        int current = priceTagIndex + _priceTagBytesLength + 1;
        bool priceUndefined = true;

        while (priceTagIndex != -1)
        {
            if (data[current] == _closeTagBytes)
            {
                ++current;
                int price = GetPrice(data, current, out int endPos);
                current += endPos;

                if (price > 0)
                {
                    if (priceUndefined)
                    {
                        priceUndefined = false;
                        performanceTickets.MinTicketPrice = price;
                    }
                    else
                    {
                        performanceTickets.MinTicketPrice = Math.Min(price, performanceTickets.MinTicketPrice);
                    }
                }
            }
            else
            {
                current += 2;
            }

           
            priceTagIndex = data.AsSpan(current, data.Length - current).IndexOf(_priceTagBytes);
            current += priceTagIndex + _priceTagBytesLength + 1;
        }

        return performanceTickets;
    }

    private async Task<IPerformanceTickets> PrivateParseKassir(byte[] data, CancellationToken cancellationToken)
    {
        if (null == data)
        {
            return new PerformanceTickets { State = TicketsState.TechnicalError, LastUpdate = DateTime.UtcNow };
        }

        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream dataStream = new MemoryStream(data);
        using IDocument document = await context.OpenAsync(req => req.Content(dataStream), cancellationToken);

        var pricePart = document.GetBody().QuerySelector(".price");
        if (pricePart == null)
            return new PerformanceTickets { State = TicketsState.TechnicalError, LastUpdate = DateTime.UtcNow };

        IPerformanceTickets performanceTickets = new PerformanceTickets { State = TicketsState.Ok };

        var prices = pricePart.TextContent
            .RemoveWhitespace()
            .Trim('\n')
            .Split('—', StringSplitOptions.RemoveEmptyEntries);

        performanceTickets.MinTicketPrice = ToInt(prices.First());

        return performanceTickets;
    }

    private int GetPrice(byte[] data, int startPricePos, out int endPos)
    {
        int endPriceTagIndex = data.AsSpan(startPricePos, data.Length - startPricePos).IndexOf(_closePriceTagBytes);
        endPos = endPriceTagIndex + _closePriceTagBytesLength + 2;

        string priceString = Encoding.UTF8.GetString(data, startPricePos, endPriceTagIndex);

        return ToInt(priceString);
    }

    private static int ToInt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default;

        return int.TryParse(value, out var ret) ? ret : default;
    }
}