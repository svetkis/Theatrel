using System;
using System.Diagnostics;
using System.Linq;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;

namespace theatrel.Lib.MihailovkyParsers
{
    internal class MihailovskyPerformanceParser : IPerformanceParser
    {
        public IPerformanceData Parse(object element, int year, int month)
        {
            try
            {
                IElement parsedElement = (IElement)element;

                var details = parsedElement.Children?.FirstOrDefault(c => c.ClassName == "detail");
                if (details == null || !details.Children.Any())
                    return null;

                string url = ProcessUrl(details.Children.FirstOrDefault(c => c.LocalName == "h2")?.Children[0]);
                string name = details.Children
                    .FirstOrDefault(c => c.LocalName == "h2")
                    ?.Children.
                    FirstOrDefault(c => c.LocalName == "a")?.TextContent.Trim();

                string ticketsUrl = null;
                string location = "Михайловский театр";
                string type = "Неопределено";

                var locationElement = details.Children.FirstOrDefault(c => c.ClassName == "place");
                if (locationElement != null)
                    location = locationElement.TextContent.Trim();

                var info = details.Children.First(c => c.ClassName == "info");
                if (info != null && info.Children.Any())
                {
                    var ticket = info.Children.FirstOrDefault(c => c.ClassName == "ticket")?.Children.FirstOrDefault();
                    ticketsUrl = GetTicketsUrl(ticket);

                    var typeElement = info.Children.FirstOrDefault(c => c.ClassName == "type f-ap");
                    if (typeElement != null)
                    {
                        string typeDescription = typeElement.TextContent.Trim();
                        type = PerformanceTypes.FirstOrDefault(type =>
                            typeDescription.Contains(type, StringComparison.OrdinalIgnoreCase)) ?? "Балет" ;
                    }
                }

                string day = parsedElement.Children
                    ?.FirstOrDefault(c => c.ClassName == "date")
                    ?.Children
                    ?.FirstOrDefault(c => c.ClassName == "day f-ap")
                    ?.TextContent;

                int.TryParse(day, out int dayResult);

                string time = parsedElement.Children
                    ?.FirstOrDefault(c => c.ClassName == "date")
                    ?.Children
                    ?.FirstOrDefault(c => c.ClassName == "time f-ap")
                    ?.Children
                    ?.FirstOrDefault()
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
}
