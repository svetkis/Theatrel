using System;
using System.Diagnostics;
using System.Linq;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;
using theatrel.Lib.Utils;

namespace theatrel.Lib.MihailovkyParsers;

internal class MihailovskyPerformanceParser : IPerformanceParser
{
    private const string Detail = "detail";
    public IPerformanceData Parse(object element, int year, int month)
    {
        try
        {
            IElement parsedElement = (IElement)element;

            IElement details = parsedElement.GetChildByPropPath(e => e.ClassName, Detail);
            if (details == null || !details.Children.Any())
                return null;

            string url = ProcessUrl(details.Children.FirstOrDefault(c => c.LocalName == "h2")?.Children.First());
            string name = details
                .Children.FirstOrDefault(c => c.LocalName == "h2")
                ?.Children.FirstOrDefault(c => c.LocalName == "a")
                ?.TextContent.Trim();

            string ticketsUrl = null;
            string location = "Михайловский театр";
            string type = "Неопределено";

            var locationElement = details.GetChildByPropPath(e => e.ClassName, "place");
            if (locationElement != null)
            {
                var tempLocation = locationElement.TextContent.Trim();
                if (!string.IsNullOrEmpty(tempLocation) && 0 != string.Compare(tempLocation, "Сцена"))
                    location = tempLocation;
            }

            var info = details.Children.First(c => c.ClassName == "info");
            if (info.Children.Any())
            {
                var ticket = info.GetChildByPropPath(e => e.ClassName, "ticket")?.Children.FirstOrDefault();
                ticketsUrl = GetTicketsUrl(ticket);

                var typeElement = info.Children.FirstOrDefault(c => c.ClassName == "type f-ap");
                if (typeElement != null)
                {
                    string typeDescription = typeElement.TextContent.Trim();
                    type = PerformanceTypes.FirstOrDefault(type =>
                        typeDescription.Contains(type, StringComparison.OrdinalIgnoreCase)) ?? "Балет" ;
                }
            }

            var dateInfo = parsedElement.GetChildByPropPath(e => e.ClassName, "date");

            string day = dateInfo
                ?.GetChildByPropPath(e => e.ClassName, "day f-ap")
                ?.TextContent;

            int.TryParse(day, out int dayResult);

            string time = dateInfo
                ?.GetChildByPropPath(e => e.ClassName, "time f-ap")
                ?.Children.FirstOrDefault()
                ?.TextContent;

            string[] splitTime = time.Split(":");

            int.TryParse(splitTime[0], out int hourResult);
            int.TryParse(splitTime[1], out int minutesResult);

            var dt = new DateTime(year, month, dayResult, hourResult-3, minutesResult, 0, 0, DateTimeKind.Utc);

            return new PerformanceData
            {
                DateTime = dt,
                Name = name,
                Url = url,
                TicketsUrl = ticketsUrl,
                Type = type,
                Location = location,
                Description = null
            };
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.Message);
            return null;
        }
    }

    private string GetTicketsUrl(IElement ticketsTButton)
    {
        if (ticketsTButton == null)
            return CommonTags.NoTicketsTag;

        string ticketsButtonContent = ticketsTButton.TextContent.Trim();

        return ticketsButtonContent switch
        {
            CommonTags.BuyTicket => ProcessUrl(ticketsTButton),
            CommonTags.WasMoved => CommonTags.WasMovedTag,
            CommonTags.NoTickets => CommonTags.NoTicketsTag,
            _ => CommonTags.NotDefinedTag
        };
    }

    private string ProcessUrl(IElement urlData)
    {
        if (null == urlData)
            return CommonTags.NotDefinedTag;

        string url = urlData.GetAttribute("href").Trim();
        if (url == CommonTags.JavascriptVoid)
            return CommonTags.NotDefinedTag;

        return url.StartsWith("/") ? $"https://mikhailovsky.ru{url}" : url;
    }

    private static readonly string[] PerformanceTypes = {"Концерт", "Балет", "Опера"};
}