using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib.Parsers
{
    internal class PerformanceParser : IPerformanceParser
    {
        public IPerformanceData Parse(object element)
        {
            try
            {
                IElement parsedElement = (IElement)element;
                IElement[] allElementChildren = parsedElement.QuerySelectorAll("*").ToArray();

                var specNameChildren = allElementChildren.FirstOrDefault(m => m.ClassName == "spec_name")?.Children;
                string dtString = allElementChildren.FirstOrDefault(m => 0 == string.Compare(m.TagName, "time", true))?.GetAttribute("datetime");

                IElement ticketsTButton = allElementChildren.FirstOrDefault(m => m.ClassName == "t_button");
                IHtmlCollection<IElement> ticketsUrlData = ticketsTButton?.Children;
                string ticketsButtonContent = ticketsTButton?.TextContent.Trim();

                string location = allElementChildren
                    .FirstOrDefault(m => 0 == string.Compare(m.TagName, "span", true) && m.GetAttribute("itemprop") == "location")?.TextContent;

                string dateString = dtString.Replace("T", " ").Replace("+", " +");
                var dt = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)
                    .ToUniversalTime();

                string name = specNameChildren.Any()
                    ? specNameChildren.Last()?.TextContent.Trim()
                    : CommonTags.NotDefinedTag;

                string url = ProcessSpectsUrl(specNameChildren);

                string ticketsUrl;

                switch (ticketsButtonContent)
                {
                    case CommonTags.BuyTicket:
                        ticketsUrl = ProcessUrl(ticketsUrlData);
                        break;
                    case CommonTags.WasMoved:
                        ticketsUrl = CommonTags.WasMovedTag;
                        Trace.TraceInformation($"{ticketsUrl} {dt:g} {name} {ticketsUrl}");
                        break;
                    case CommonTags.NoTickets:
                        ticketsUrl = CommonTags.NoTicketsTag;
                        Trace.TraceInformation($"{ticketsUrl} {dt:g} {name} {ticketsUrl}");
                        break;
                    default:
                        ticketsUrl = CommonTags.NotDefinedTag;
                        Trace.TraceInformation($"{ticketsUrl} {dt:g} {name} {ticketsUrl}");
                        break;
                }

                return new PerformanceData
                {
                    DateTime = dt,
                    Name = name,
                    Url = url,
                    TicketsUrl = ticketsUrl,
                    Type = GetType(parsedElement.ClassList.ToArray()),
                    Location = GetLocation(location.Trim()),
                };
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                return null;
            }
        }

        private string ProcessUrl(IHtmlCollection<IElement> urlData)
        {
            if (!urlData.Any())
                return CommonTags.NotDefinedTag;

            string url = urlData.First().GetAttribute("href").Trim();
            if (url == CommonTags.JavascriptVoid)
                return CommonTags.NotDefinedTag;

            return url.StartsWith("//") ? $"https:{url}" : url;
        }

        private string ProcessSpectsUrl(IHtmlCollection<IElement> urlData)
        {
            if (!urlData.Any())
                return CommonTags.NotDefinedTag;

            string url = urlData.FirstOrDefault(m => m.GetAttribute("itemprop") == "url")?.GetAttribute("href").Trim();
            if (string.IsNullOrEmpty(url) || url == CommonTags.JavascriptVoid)
                return CommonTags.NotDefinedTag;

            return url.StartsWith("/") ? $"https://www.mariinsky.ru{url}" : url;
        }

        private static readonly Lazy<IDictionary<string, string>> PerformanceTypes
            = new Lazy<IDictionary<string, string>>(() => new Dictionary<string, string>
            {
            {"c_opera", "Опера"},
            {"c_concert", "Концерт" },
            {"c_ballet", "Балет" }
        }, true);

        private static readonly Lazy<IDictionary<string, string>> PerformanceLocations
            = new Lazy<IDictionary<string, string>>(() => new Dictionary<string, string>
            {
                {"Мариинский театр", "Мариинский театр"},
                {"Концертный зал", "Концертный зал (Мариинский театр)" },
                {"Мариинский-2", "Мариинский-2" }
            }, true);

        public string GetType(string[] types)
        {
            foreach (var type in types)
            {
                if (PerformanceTypes.Value.ContainsKey(type))
                    return PerformanceTypes.Value[type];
            }

            return types.Reverse().Skip(1).First();
        }

        public string GetLocation(string location)
            => PerformanceLocations.Value.ContainsKey(location) ? PerformanceLocations.Value[location] : location;
    }
}
