using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Cast;

namespace theatrel.Lib.MariinskyPlaybill
{
    internal class PlayBillResolver : IPlayBillDataResolver
    {
        private readonly IPlaybillParser _playbillParser;
        private readonly ITicketsParser _ticketsParser;
        private readonly IFilterService _filterChecker;
        private readonly IPerformanceCastParser _performanceCastParser;

        public PlayBillResolver(IPlaybillParser playbillParser, ITicketsParser ticketsParser, IFilterService filterChecker, IPerformanceCastParser performanceCastParser)
        {
            _playbillParser = playbillParser;
            _ticketsParser = ticketsParser;
            _filterChecker = filterChecker;
            _performanceCastParser = performanceCastParser;
        }

        public async Task<IPerformanceData[]> RequestProcess(IPerformanceFilter filter, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("PlayBillResolver.RequestProcess started");
            DateTime[] months = filter.StartDate.GetMonthsBetween(filter.EndDate);

            List<IPerformanceData> performances = new List<IPerformanceData>();

            foreach (var dateTime in months)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string content = await Request(dateTime, cancellationToken);
                performances.AddRange(await _playbillParser.Parse(content, cancellationToken));
            }

            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<IPerformanceData> filtered = performances
                .Where(item => item != null && _filterChecker.IsDataSuitable(item, filter)).ToArray();

            Task[] resolvePricesTasks = filtered
                .Select(item => Task.Run(async () =>
                {
                    item.Cast = await _performanceCastParser.ParseFromUrl(item.Url, cancellationToken);
                    var tickets = await _ticketsParser.ParseFromUrl(item.TicketsUrl, cancellationToken);
                    item.State = tickets.State;
                    item.MinPrice = tickets.GetMinPrice();

                }, cancellationToken)).ToArray();

            await Task.WhenAll(resolvePricesTasks.ToArray());

            Trace.TraceInformation(" PlayBillResolver.RequestProcess finished");
            return filtered.ToArray();
        }

        private async Task<string> Request(DateTime date, CancellationToken cancellationToken)
        {
            string url = $"https://www.mariinsky.ru/ru/playbill/playbill/?year={date.Year}&month={date.Month}";

            RestClient client = new RestClient(url);

            RestRequest request = new RestRequest(Method.GET);
            request.AddHeader("Host", "www.mariinsky.ru");
            request.AddHeader("X-Requested-With", "XMLHttpRequest");

            IRestResponse response = await client.ExecuteAsync(request, cancellationToken);
            return response.Content;
        }
    }
}
