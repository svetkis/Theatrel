using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Enums;

namespace theatrel.Lib.Playbill
{
    internal class PlayBillResolver : IPlayBillDataResolver
    {
        private readonly IFilterService _filterChecker;

        private readonly Func<Theatre, IPlaybillParser> _playbillParserFactory;
        private readonly Func<Theatre, IPerformanceParser> _performanceParserFactory;
        private readonly Func<Theatre, ITicketsParser> _ticketsParserFactory;
        private readonly Func<Theatre, IPerformanceCastParser> _castParserFactory;

        public PlayBillResolver(Func<Theatre, IPlaybillParser> playbillParserFactory,
            Func<Theatre, ITicketsParser> ticketsParserFactory,
            Func<Theatre, IPerformanceCastParser> castParserFactory,
            Func<Theatre, IPerformanceParser> performanceParserFactory,
            IFilterService filterChecker)
        {
            _playbillParserFactory = playbillParserFactory;
            _ticketsParserFactory = ticketsParserFactory;
            _performanceParserFactory = performanceParserFactory;
            _castParserFactory = castParserFactory;

            _filterChecker = filterChecker;
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

                string content = await Request((Theatre)theatre, dateTime, cancellationToken);
                if (string.IsNullOrEmpty(content))
                    continue;

                performances.AddRange(await playbillParser.Parse(content, performanceParser,
                    dateTime.Year, dateTime.Month, cancellationToken));
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

            Trace.TraceInformation(" PlayBillResolver.RequestProcess finished");
            return filtered.ToArray();
        }

        private async Task<string> Request(Theatre id, DateTime date, CancellationToken cancellationToken)
        {
            string url = id switch
            {
                Theatre.Mariinsky =>
                    $"https://www.mariinsky.ru/ru/playbill/playbill/?year={date.Year}&month={date.Month}",
                Theatre.Mikhailovsky => $"https://mikhailovsky.ru/afisha/performances/{date.Year}/{date.Month}/",
                _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
            };

            RestClient client = new RestClient(url);

            RestRequest request = new RestRequest(Method.GET);

            IRestResponse response = await client.ExecuteAsync(request, cancellationToken);

            if (response.ResponseUri == null || !string.Equals(response.ResponseUri.AbsoluteUri, url))
                return null;

            if (!response.ContentType.Contains("1251"))
                return response.Content;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("windows-1251");
            string result = Encoding.UTF8.GetString(Encoding.Convert(encoding, Encoding.UTF8, response.RawBytes))
                .Replace("windows-1251", "utf-8");

            return result;
        }
    }
}
