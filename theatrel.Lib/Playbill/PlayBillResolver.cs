using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

namespace theatrel.Lib.Playbill;

internal class PlayBillResolver : IPlayBillDataResolver
{
    private readonly IFilterService _filterChecker;
    private readonly IEncodingService _encodingService;

    private readonly Func<Theatre, IPlaybillParser> _playbillParserFactory;
    private readonly Func<Theatre, IPerformanceParser> _performanceParserFactory;
    private readonly Func<Theatre, ITicketsParser> _ticketsParserFactory;
    private readonly Func<Theatre, IPerformanceCastParser> _castParserFactory;

    public PlayBillResolver(Func<Theatre, IPlaybillParser> playbillParserFactory,
        Func<Theatre, ITicketsParser> ticketsParserFactory,
        Func<Theatre, IPerformanceCastParser> castParserFactory,
        Func<Theatre, IPerformanceParser> performanceParserFactory,
        IFilterService filterChecker,
        IEncodingService encodingService)
    {
        _playbillParserFactory = playbillParserFactory;
        _ticketsParserFactory = ticketsParserFactory;
        _performanceParserFactory = performanceParserFactory;
        _castParserFactory = castParserFactory;

        _filterChecker = filterChecker;
        _encodingService = encodingService;
    }

    public async Task<IPerformanceData[]> RequestProcess(int theatre, IPerformanceFilter filter, CancellationToken cancellationToken)
    {
        Trace.TraceInformation("PlayBillResolver.RequestProcess started");

        var playbillParser = _playbillParserFactory((Theatre)theatre);
        var performanceParser = _performanceParserFactory((Theatre)theatre);
        var performanceCastParser = _castParserFactory((Theatre) theatre);
        var ticketsParser = _ticketsParserFactory((Theatre)theatre);

        DateTime[] months = filter.StartDate.GetMonthsBetween(filter.EndDate);

        List<IPerformanceData> performances = new List<IPerformanceData>();

        foreach (var dateTime in months)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await Request((Theatre)theatre, dateTime, cancellationToken);
            if (content == null || !content.Any())
                continue;

            performances.AddRange(await playbillParser.Parse(content, performanceParser, dateTime.Year, dateTime.Month, cancellationToken));
        }

        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<IPerformanceData> filtered = performances
            .Where(item => item != null && _filterChecker.IsDataSuitable(item, filter)).ToArray();

        Task[] resolvePricesTasks = filtered
            .Select(item => Task.Run(async () =>
            {
                var tickets = await ticketsParser.ParseFromUrl(item.TicketsUrl, cancellationToken);
                item.State = tickets.State;
                item.MinPrice = tickets.GetMinPrice();
            }, cancellationToken)).ToArray();

        await Task.WhenAll(resolvePricesTasks.ToArray());


        if (theatre == (int)Theatre.Mariinsky)
        {
            Task[] resolveCastTasks = filtered
                .Select(item => Task.Run(async () =>
                {
                    item.Cast = await performanceCastParser.ParseFromUrl(item.Url, item.State == TicketsState.PerformanceWasMoved, cancellationToken);
                }, cancellationToken)).ToArray();

            await Task.WhenAll(resolveCastTasks.ToArray());
        }

        Trace.TraceInformation("PlayBillResolver.RequestProcess finished");
        return filtered.ToArray();
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