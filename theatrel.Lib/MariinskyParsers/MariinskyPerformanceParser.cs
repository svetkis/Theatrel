using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib.MariinskyParsers;

internal class MariinskyPerformanceParser : IPerformanceParser
{
    public IPerformanceData Parse(object element, int year, int month)
    {
        try
        {
            IElement parsedElement = (IElement)element;

            string dateString = parsedElement.QuerySelector("time")?.GetAttribute("datetime")
                ?.Replace("T", " ")
                .Replace("+", " +");

            if (dateString == null)
                return null;

            var dt = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)
                .ToUniversalTime();

            IHtmlCollection<IElement> specNameChildren = parsedElement
                .QuerySelector("div.spec_name")
                ?.Children;

            IElement ticketsTButton = parsedElement.QuerySelector("div.t_button");

            string location = parsedElement.QuerySelectorAll("span")
                .FirstOrDefault(m => m.GetAttribute("itemprop") == "location")
                ?.TextContent.Trim();

            string name = specNameChildren.Any()
                ? specNameChildren.Last()?.TextContent.Trim()
                : CommonTags.NotDefinedTag;

            var statusChildren = parsedElement.QuerySelector("div.status")?.Children;
            string status = statusChildren != null && statusChildren.Any()
                ? statusChildren.Last().TextContent.Trim()
                : null;

            string url = GetUrl(specNameChildren);

            string ticketsUrl = GetTicketsUrl(ticketsTButton);

            IHtmlCollection<IElement> descrElements = parsedElement
                           .QuerySelector("div.descr")
                           ?.Children;

            string descr = descrElements.Any()
                ? descrElements.Last()?.InnerHtml
                : null;

            return new PerformanceData
            {
                DateTime = dt,
                Name = name,
                Url = url,
                TicketsUrl = ticketsUrl,
                Type = GetType(parsedElement.ClassList.ToArray()),
                Location = GetLocation(location),
                Description = status,
                CastFromPlaybill = descr,
            };
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.Message);
            return null;
        }
    }

    private static string GetTicketsUrl(IElement ticketsTButton)
    {
        string ticketsButtonContent = ticketsTButton?.TextContent.Trim();

        return ticketsButtonContent switch
        {
            CommonTags.BuyTicket => ProcessUrl(ticketsTButton.Children),
            CommonTags.WasMoved => CommonTags.WasMovedTag,
            CommonTags.NoTickets => CommonTags.NoTicketsTag,
            _ => CommonTags.NotDefinedTag
        };
    }

    private static string ProcessUrl(IHtmlCollection<IElement> urlData)
    {
        if (!urlData.Any())
            return CommonTags.NotDefinedTag;

        string url = urlData.First().GetAttribute("href").Trim();
        if (url == CommonTags.JavascriptVoid)
            return CommonTags.NotDefinedTag;

        return url.StartsWith("//") ? $"https:{url}" : url;
    }

    private static string GetUrl(IHtmlCollection<IElement> urlData)
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
            {"c_ballet", "Балет" },
            {"c_", "Балет" } //to do read it from description
        }, true);

    private static readonly Lazy<IDictionary<string, string>> PerformanceLocations
        = new(() => new Dictionary<string, string>
        {
            {"Мариинский театр", "Мариинский театр"},
            {"Концертный зал", "Концертный зал (Мариинский театр)" },
            {"Мариинский-2", "Мариинский-2" }
        }, true);

    private static string GetType(string[] types)
    {
        var type = types.FirstOrDefault(x => PerformanceTypes.Value.ContainsKey(x));
        return type != null ? PerformanceTypes.Value[type] : types.Reverse().Skip(1).First();
    }

    private static string GetLocation(string location)
        => PerformanceLocations.Value.ContainsKey(location) ? PerformanceLocations.Value[location] : location;
}