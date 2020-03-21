using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;

namespace theatrel.Lib.Parsers
{
    public class PerformanceParser: IPerformanceParser
    {
        public IPerformanceData Parse(AngleSharp.Dom.IElement element)
        {
            try
            {
                AngleSharp.Dom.IElement[] allElementChildren = element.QuerySelectorAll("*").ToArray();

                var specName = allElementChildren.FirstOrDefault(m => m.ClassName == "spec_name");
                string dtString = allElementChildren.FirstOrDefault(m => 0 == string.Compare(m.TagName, "time", true))?.GetAttribute("datetime");
                var urlData = allElementChildren.FirstOrDefault(m => m.ClassName == "t_button")?.Children;
                string url = ProcessUrl(urlData);

                string location = allElementChildren
                    .FirstOrDefault(m => 0 == string.Compare(m.TagName, "span", true) && m.GetAttribute("itemprop") == "location")?.TextContent;

                return new PerformanceData()
                {
                    DateTime = DateTime.Parse(dtString),
                    Name = specName.Children.Any() ? specName?.Children?.Last()?.TextContent : CommonTags.NotDefined,
                    Url = url,
                    Type = GetType(element.ClassList.ToArray()),
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

            string url = urlData?.First().GetAttribute("href");
            if (url == CommonTags.JavascriptVoid)
                return CommonTags.NotDefined;

            if (url.StartsWith("//"))
                return $"https:{url}";

            return url;
        }

        private static Lazy<IDictionary<string, string>> perfomanceTypes
            = new Lazy<IDictionary<string, string>>(() => new Dictionary<string, string>()
        {
            {"c_opera", "Опера"},
            {"c_concert", "Концерт" },
            {"c_ballet", "Балет" }
        }, true);

        public string GetType(string[] types)
        {
            foreach(var type in types)
            {
                if (perfomanceTypes.Value.ContainsKey(type))
                    return perfomanceTypes.Value[type];
            }

            return types.Reverse().Skip(1).First();
        }
    }
}
