using AngleSharp.Dom;
using System.Linq;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;
using theatrel.Lib.Utils;

namespace theatrel.Lib.Parsers
{
    public class TicketParser : ITicketParser
    {
        private class Ticket : ITicket
        {
            public string Id { get; set; }
            public string Region { get; set; }
            public string Side { get; set; }
            public string Row { get; set; }
            public string Place { get; set; }
            public int MinPrice { get; set; }
        }

        private string ControlContent(string data) => data == null || data.Contains('<') ? string.Empty : data.Trim();

        public ITicket Parse(IElement ticket)
        {
            IElement[] allTicketChildren = ticket.QuerySelectorAll("*").ToArray();

            return new Ticket()
            {
                Id = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "ID")?.TextContent),
                Region = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "TREGION")?.TextContent),
                Side = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "TSIDE")?.TextContent),
                Row = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "TROW")?.TextContent),
                Place = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "TPLACE")?.TextContent),
                MinPrice = GetMinPrice(allTicketChildren)
            };
        }

        private int GetMinPrice(IElement[] data)
        {
            var prices = new[]
                {
                    Helper.ToInt(data.FirstOrDefault(m => 0 == string.Compare(m.TagName, "TPRICE1", true))?.TextContent),
                    Helper.ToInt(data.FirstOrDefault(m => 0 == string.Compare(m.TagName, "TPRICE2", true))?.TextContent),
                    Helper.ToInt(data.FirstOrDefault(m => 0 == string.Compare(m.TagName, "TPRICE3", true))?.TextContent)
                }.Where(price => price > 0);

            return !prices.Any() ? 0 : prices.Min();
        }
    }
}
