using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;

namespace theatrel.Lib
{
    public class PlayBillResolver : IPlayBillDataResolver
    {
        private IPlayBillParser _playBillParser;
        private ITicketsParser _ticketParser;

        public PlayBillResolver(IPlayBillParser playBillParser, ITicketsParser ticketParser)
        {
            _playBillParser = playBillParser;
            _ticketParser = ticketParser;
        }

        public async Task<IPerformanceData[]> RequestProcess(DateTime startDate, DateTime endDate, IPerformanceFilter filter)
        {
            string content = await Request(startDate);

            IPerformanceData[] perfomances = _playBillParser.Parse(content).GetAwaiter().GetResult();

            IList<Task> tasks = new List<Task>();

            var filtredList = perfomances.Where(item => filter.Filter(item));

            foreach (var item in filtredList)
                tasks.Add(Task.Run(async () => item.Tickets = await _ticketParser.ParseFromUrl(item.Url)));

            Task.WaitAll(tasks.ToArray());

            Trace.TraceInformation("Parsing finished");
            Trace.TraceInformation("");

            return filtredList.ToArray();
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
