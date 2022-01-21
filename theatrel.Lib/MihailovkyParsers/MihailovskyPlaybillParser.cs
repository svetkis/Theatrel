using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib.MihailovkyParsers;

public class MihailovskyPlaybillParser : IPlaybillParser
{
    private readonly IPerformanceCastParser _castParser = new MihailovskyCastParser();

    public async Task<IPerformanceData[]> Parse(string playbill, IPerformanceParser performanceParser,
        int year, int month, CancellationToken cancellationToken)
    {
        IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        IDocument document = await context.OpenAsync(req => req.Content(playbill), cancellationToken);

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

            parsed.Cast = await _castParser.Parse(persons, cancellationToken);
            performances.Add(parsed);
        }

        return performances.ToArray();
    }
}