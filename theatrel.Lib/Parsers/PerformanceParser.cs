using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib.Parsers
{
    public class PerformanceParser : IPerformanceParser
    {
        public IPerformanceData Parse(object element)
        {
            try
            {
                AngleSharp.Dom.IElement parsedElement = (AngleSharp.Dom.IElement)element;
                AngleSharp.Dom.IElement[] allElementChildren = parsedElement.QuerySelectorAll("*").ToArray();

                var specName = allElementChildren.FirstOrDefault(m => m.ClassName == "spec_name");
                string dtString = allElementChildren.FirstOrDefault(m => 0 == string.Compare(m.TagName, "time", true))?.GetAttribute("datetime");
                var urlData = allElementChildren.FirstOrDefault(m => m.ClassName == "t_button")?.Children;
                string url = ProcessUrl(urlData);

                string location = allElementChildren
                    .FirstOrDefault(m => 0 == string.Compare(m.TagName, "span", true) && m.GetAttribute("itemprop") == "location")?.TextContent;

                string dateString = dtString.Replace("T", " ").Replace("+", " +");
                var dt = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)
                    .ToUniversalTime();

                return new PerformanceData
                {
                    DateTime = dt,
                    Name = specName.Children.Any() ? specName.Children?.Last()?.TextContent : CommonTags.NotDefined,
                    Url = url,
                    Type = GetType(parsedElement.ClassList.ToArray()),
                    Location = location,
                };
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                return null;
            }
        }

        private string ProcessUrl(AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> urlData)
        {
            if (!urlData.Any())
                return CommonTags.NotDefined;

            string url = urlData.First().GetAttribute("href");
            if (url == CommonTags.JavascriptVoid)
                return CommonTags.NotDefined;

            if (url.StartsWith("//"))
                return $"https:{url}";

            return url;
        }

        private static readonly Lazy<IDictionary<string, string>> performanceTypes
            = new Lazy<IDictionary<string, string>>(() => new Dictionary<string, string>()
        {
            {"c_opera", "Опера"},
            {"c_concert", "Концерт" },
            {"c_ballet", "Балет" }
        }, true);

        public string GetType(string[] types)
        {
            foreach (var type in types)
            {
                if (performanceTypes.Value.ContainsKey(type))
                    return performanceTypes.Value[type];
            }

            return types.Reverse().Skip(1).First();
        }
    }
}
