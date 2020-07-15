using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;

namespace theatrel.Lib
{
    public class PlayBillResolver : IPlayBillDataResolver
    {
        private readonly IPlayBillParser _playBillParser;
        private readonly ITicketsParser _ticketParser;
        private readonly IFilterChecker _filterChecker;

        public PlayBillResolver(IPlayBillParser playBillParser, ITicketsParser ticketParser, IFilterChecker filterChecker)
        {
            _playBillParser = playBillParser;
            _ticketParser = ticketParser;
            _filterChecker = filterChecker;
        }

        public async Task<IPerformanceData[]> RequestProcess(DateTime startDate, DateTime endDate, IPerformanceFilter filter, CancellationToken cancellationToken)
        {
            string content = await Request(startDate);

            IPerformanceData[] performances = await _playBillParser.Parse(content, cancellationToken);

            IList<Task> tasks = new List<Task>();

            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<IPerformanceData> filtered = performances.Where(item => _filterChecker.IsDataSuitable(item, filter)).ToArray();

            foreach (var item in filtered)
                tasks.Add(Task.Run(async () => item.Tickets = await _ticketParser.ParseFromUrl(item.Url, cancellationToken), cancellationToken));

            await Task.WhenAll(tasks.ToArray());

            Trace.TraceInformation("Parsing finished");

            return filtered.ToArray();
        }

        private async Task<string> Request(DateTime date)
        {
            string url = $"https://www.mariinsky.ru/ru/playbill/playbill/?year={date.Year}&month={date.Month}";

            RestClient client = new RestClient(url);
            RestRequest request = new RestRequest(Method.GET);

            request.AddHeader("Host", "www.mariinsky.ru");
            request.AddHeader("X-Requested-With", "XMLHttpRequest");

            IRestResponse response = await client.ExecuteAsync(request);
            return response.Content;
        }
    }
}
