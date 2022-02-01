using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;
using theatrel.Lib.Utils;

namespace theatrel.Lib.MariinskyParsers;

public class MariinskyPlaybillParser : IPlaybillParser
{
    public async Task<IPerformanceData[]> Parse(byte[] playbill, IPerformanceParser performanceParser,
        int year, int month, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream playbillStream = new MemoryStream(playbill);
        using IDocument document = await context.OpenAsync(req => req.Content(playbillStream), cancellationToken);

        var dayRowList = document.GetBody().QuerySelector("div#afisha")?.QuerySelectorAll("div.day_row");
        if(dayRowList == null)
            return Array.Empty<IPerformanceData>();

        ConcurrentBag<IPerformanceData> performances = new ConcurrentBag<IPerformanceData>();
        foreach (var row in dayRowList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IElement spects = row.QuerySelector("div.spects");
            if (spects == null)
                continue;

            Parallel.ForEach(spects.Children, new ParallelOptions { CancellationToken = cancellationToken },
                performance =>
                {
                    IPerformanceData parsed = performanceParser.Parse(performance, 0, 0);
                    if (null != parsed)
                        performances.Add(parsed);
                });
        }

        return performances.ToArray();
    }
}