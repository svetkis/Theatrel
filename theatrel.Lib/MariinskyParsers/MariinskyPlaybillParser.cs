using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib.MariinskyParsers;

public class MariinskyPlaybillParser : IPlaybillParser
{
    public async Task<IPerformanceData[]> Parse(byte[] playbill, IPerformanceParser performanceParser,
        int year, int month, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream playbillStream = new MemoryStream(playbill);
        using IDocument document = await context.OpenAsync(req => req.Content(playbillStream), cancellationToken);

        IList<IPerformanceData> performances = new List<IPerformanceData>();
        var dayRowList = document.All.Where(m => CheckClassListContains(m, DayRow));

        foreach (var row in dayRowList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IElement spects = GetFirstClassFromAllChildren(row, Spects);
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

    private static IElement GetFirstClassFromAllChildren(IElement element, string[] classNames)
    {
        IHtmlCollection<IElement> allElementChildren = element.QuerySelectorAll("*");
        return allElementChildren.FirstOrDefault(m => CheckClassList(m, classNames));
    }

    private static readonly string[] DayRow = { "row", "day_row" };
    private static readonly string[] Spects = { "col-md-10", "spects" };

    private static bool CheckClassList(IElement element, string[] tags)
        => element.ClassList.Intersect(tags).Count() == tags.Length;

    private static bool CheckClassListContains(IElement element, string[] tags)
        => tags.All(tag => element.ClassList.Contains(tag));
}