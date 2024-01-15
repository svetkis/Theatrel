using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.EncodingService;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Enums;
using theatrel.Lib.Utils;

namespace theatrel.Lib.Playbill;

internal class PlayBillResolver : IPlayBillDataResolver
{
    private readonly IEncodingService _encodingService;

    private readonly Func<Theatre, IPlaybillParser> _playbillParserFactory;
    private readonly Func<Theatre, IPerformanceParser> _performanceParserFactory;
    private readonly Func<Theatre, ITicketsParser> _ticketsParserFactory;
    private readonly Func<Theatre, IPerformanceCastParser> _castParserFactory;

    public PlayBillResolver(Func<Theatre, IPlaybillParser> playbillParserFactory,
        Func<Theatre, ITicketsParser> ticketsParserFactory,
        Func<Theatre, IPerformanceCastParser> castParserFactory,
        Func<Theatre, IPerformanceParser> performanceParserFactory,
        IEncodingService encodingService)
    {
        _playbillParserFactory = playbillParserFactory;
        _ticketsParserFactory = ticketsParserFactory;
        _performanceParserFactory = performanceParserFactory;
        _castParserFactory = castParserFactory;

        _encodingService = encodingService;
    }

    public async Task<IPerformanceData[]> RequestProcess(int theatre, IPerformanceFilter filter, CancellationToken cancellationToken)
    {
        var playbillParser = _playbillParserFactory((Theatre)theatre);
        var performanceParser = _performanceParserFactory((Theatre)theatre);

        DateTime[] months = filter.StartDate.GetMonthsBetween(filter.EndDate);

        List<IPerformanceData> performances = new List<IPerformanceData>();

        foreach (var dateTime in months)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(5000);

            var content = await Request((Theatre)theatre, dateTime, cancellationToken);
            if (content == null || !content.Any())
                continue;

            performances.AddRange(await playbillParser.Parse(content, performanceParser, dateTime.Year, dateTime.Month, cancellationToken));
        }

        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<IPerformanceData> filtered = performances
            .Where(item => item != null && CheckOnlyDate(item.DateTime, filter))
            .ToArray();

        return filtered.ToArray();
    }

    private bool CheckOnlyDate(DateTime when, IPerformanceFilter filter)
    {
        if (filter == null)
            return true;

        return filter.StartDate <= when && filter.EndDate >= when;
    }

    public async Task AdditionalProcess(int theatre, IPerformanceData[] performances, CancellationToken cancellationToken)
    {
        var performanceCastParser = _castParserFactory((Theatre)theatre);
        ITicketsParser ticketsParser = _ticketsParserFactory((Theatre)theatre);

        int delay = theatre == (int)Theatre.Mariinsky ? 30000 : 1000;

        await Parallel.ForEachAsync(
            performances,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism  = 1},
            async (performance, ctx) =>
            {
                var tickets = await ticketsParser.ParseFromUrl(performance.TicketsUrl, ctx);
                performance.State = tickets.State;
                performance.MinPrice = tickets.MinTicketPrice;

                await Task.Delay(delay);
            });

        if (theatre == (int)Theatre.Mariinsky)
        {
            await Parallel.ForEachAsync(
                performances,
                new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 1 },
                async (performance, ctx) =>
                {
                    if (performance.State == TicketsState.PerformanceWasMoved)
                        return;

                    performance.Cast = await performanceCastParser.ParseFromUrl(
                        performance.Url,
                        performance.CastFromPlaybill,
                        false,
                        ctx);

                    await Task.Delay(delay);
                });
        }
    }

    private async Task<byte[]> Request(Theatre id, DateTime date, CancellationToken cancellationToken)
    {
        string url = id switch
        {
            Theatre.Mariinsky =>
                $"https://www.mariinsky.ru/ru/playbill/playbill/?year={date.Year}&month={date.Month}",
            Theatre.Mikhailovsky => $"https://mikhailovsky.ru/afisha/performances/{date.Year}/{date.Month}/",
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };

        using RestClient client = new RestClient(url);

        RestRequest request = new RestRequest {Method = Method.Get};

        RestResponse response = await client.ExecuteAsync(request, cancellationToken);

        if (!string.Equals(response.ResponseUri?.AbsoluteUri, url))
            return null;

        return _encodingService.ProcessBytes(response.RawBytes);
    }
}