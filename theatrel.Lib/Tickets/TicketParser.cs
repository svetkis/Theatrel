using System.Linq;
using System.Threading;
using AngleSharp.Dom;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Utils;

namespace theatrel.Lib.Tickets
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

        public ITicket Parse(object ticket, CancellationToken cancellationToken)
        {
            IElement parsedTicket = (IElement)ticket;

            IElement[] allTicketChildren = parsedTicket.QuerySelectorAll("*").ToArray();

            var tickets = new Ticket
            {
                Id = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "ID")?.TextContent),
                Region = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "TREGION")?.TextContent),
                Side = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "TSIDE")?.TextContent),
                Row = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "TROW")?.TextContent),
                Place = ControlContent(allTicketChildren.FirstOrDefault(m => m.TagName == "TPLACE")?.TextContent),
                MinPrice = GetMinPrice(allTicketChildren)
            };

            return tickets;
        }

        //We can see three or two prices, but we are interested only about the lowest one, because it is correct one for RF citizens
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
