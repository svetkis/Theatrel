using AngleSharp;
using AngleSharp.Dom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;

namespace theatrel.Lib.Parsers
{
    public class PlayBillParser : IPlayBillParser
    {
        public IPerformanceParser PerformanceParser { get; set; }

        public PlayBillParser(IPerformanceParser performanceParser)
        {
            PerformanceParser = performanceParser;
        }

        public async Task<IPerformanceData[]> Parse(string playbill)
        {
            var context = BrowsingContext.New(Configuration.Default);
            IDocument document = await context.OpenAsync(req => req.Content(playbill));

            IList<IPerformanceData> performances = new List<IPerformanceData>();
            var dayRowList = document.All.Where(m => CheckClassListContains(m, DayRow));

            foreach (var row in dayRowList)
            {
                string day = GetClassValueFromAllChildren(row, "d");
                int dayValue;
                int.TryParse(day, out dayValue);

                IElement spects = GetFirstClassFromAllChildren(row, Spects);
                if (spects == null)
                    continue;

                foreach (var perfomance in spects.Children)
                {
                    var parsed = PerformanceParser.Parse(perfomance);
                    if (null != parsed)
                        performances.Add(parsed);
                }
            }

            return performances.ToArray();
        }

        private static string GetClassValueFromAllChildren(IElement element, string className)
        {
            var allElementChildren = element.QuerySelectorAll("*");
            return allElementChildren.FirstOrDefault(m => m.ClassName == className)?.TextContent;
        }

        private static IElement GetFirstClassFromAllChildren(IElement element, string[] classNames)
        {
            var allElementChildren = element.QuerySelectorAll("*");
            return allElementChildren.FirstOrDefault(m => CheckClassList(m, classNames));
        }

        private static string[] DayRow = { "row", "day_row" };
        private static string[] Spects = { "col-md-10", "spects" };

        private static bool CheckClassList(IElement element, string[] tags)
            => element.ClassList.Intersect(tags).Count() == tags.Count();

        private static bool CheckClassListContains(IElement element, string[] tags)
            => tags.All(tag => element.ClassList.Contains(tag));
    }
}
