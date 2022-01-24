using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Helpers;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;
using theatrel.Lib.Cast;

namespace theatrel.Lib.MihailovkyParsers;

public class MihailovskyPlaybillParser : IPlaybillParser
{
    private readonly IPerformanceCastParser _castParser;

    public MihailovskyPlaybillParser(IPageRequester pageRequester)
    {
        _castParser = new MihailovskyCastParser(pageRequester);
    }

    public async Task<IPerformanceData[]> Parse(byte[] playbill, IPerformanceParser performanceParser,
        int year, int month, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream streamPlaybill = new MemoryStream(playbill);
        using IDocument document = await context.OpenAsync(req => req.Content(streamPlaybill), cancellationToken);

        IList<IPerformanceData> performances = new List<IPerformanceData>();

        IElement afisha = document.All.FirstOrDefault(m => m.ClassName == "afisha");

        var performanceList = afisha?.Children
            .FirstOrDefault(c => c.Id == "afisha_performance_list")
            ?.Children
            ?.FirstOrDefault(c => c.Id == "afisha_performance_list_container")
            ?.Children
            ?.FirstOrDefault(c => c.Id == "list");

        if (performanceList == null)
            return performances.ToArray();

        int dayValue = 0;
        foreach (var performanceDiv in performanceList.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string day = performanceDiv.Children
                ?.FirstOrDefault(c => c.ClassName=="date")
                ?.Children
                ?.FirstOrDefault(c => c.ClassName == "day f-ap")
                ?.TextContent;

            if (day == null)
                continue;

            int.TryParse(day, out int newDayValue);
            if (newDayValue < dayValue)
                continue;

            dayValue = newDayValue;

            IPerformanceData parsed = performanceParser.Parse(performanceDiv, year, month);
            if (null == parsed)
                continue;

            string persons = performanceDiv.Children
                ?.FirstOrDefault(c => c.ClassName == "detail")
                ?.Children
                ?.FirstOrDefault(c => c.ClassName == "info")
                ?.Children
                ?.FirstOrDefault(c => c.ClassName == "persons")
                ?.InnerHtml;

            parsed.Cast = string.IsNullOrEmpty(persons) ? new PerformanceCast() : await _castParser.Parse(Encoding.UTF8.GetBytes(persons), cancellationToken);
            performances.Add(parsed);
        }

        return performances.ToArray();
    }
}