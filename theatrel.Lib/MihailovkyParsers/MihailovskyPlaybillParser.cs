using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Helpers;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;
using theatrel.Lib.Cast;
using theatrel.Lib.Utils;

namespace theatrel.Lib.MihailovkyParsers;

public class MihailovskyPlaybillParser : IPlaybillParser
{
    private readonly IPerformanceCastParser _castParser;

    private readonly string[] _afishaContentPath = { "afisha_performance_list", "afisha_performance_list_container", "list" };
    private readonly string[] _dayPath = { "date", "day f-ap" };
    private readonly string[] _personsPath = { "detail", "info", "persons" };
    private readonly string[] _afishaPath = { "inner", "layoutWrapper", "inside", "i-wrapper", "afisha" };

    public MihailovskyPlaybillParser(IPageRequester pageRequester)
    {
        _castParser = new MihailovskyCastParser(pageRequester);
    }

    public async Task<IPerformanceData[]> Parse(byte[] playbill, IPerformanceParser performanceParser,
        int year, int month, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream playbillStream = new MemoryStream(playbill);
        using IDocument document = await context.OpenAsync(req => req.Content(playbillStream), cancellationToken);

        IElement afisha = document.GetBody().GetChildByPropPath(e => e.ClassName, _afishaPath);

        var performanceList = afisha.GetChildByPropPath(e => e.Id, _afishaContentPath);

        if (performanceList == null)
            return Array.Empty<IPerformanceData>();

        int dayValue = 0;
        ConcurrentBag<IPerformanceData> performances = new ConcurrentBag<IPerformanceData>();

        await Parallel.ForEachAsync(
            performanceList.Children,
            new ParallelOptions { CancellationToken = cancellationToken },
            async (performanceDiv, ctx) =>
            {
                ctx.ThrowIfCancellationRequested();

                string day = performanceDiv.GetChildByPropPath(e => e.ClassName, _dayPath)?.TextContent;

                if (day == null)
                    return;

                int.TryParse(day, out int newDayValue);
                if (newDayValue < dayValue)
                    return;

                dayValue = newDayValue;

                IPerformanceData parsed = performanceParser.Parse(performanceDiv, year, month);
                if (null == parsed)
                    return;

                string personsHtml = performanceDiv.GetChildByPropPath(e => e.ClassName, _personsPath)?.InnerHtml;

                parsed.Cast = string.IsNullOrEmpty(personsHtml)
                    ? new PerformanceCast()
                    : await _castParser.ParseText(personsHtml, cancellationToken);

                performances.Add(parsed);
            });

        return performances.ToArray();
    }
}