using System.Threading;

namespace theatrel.Interfaces.Parsers
{
    public interface ITicketParser : IDIRegistrable
    {
        ITicket Parse(object ticket, CancellationToken cancellationToken);
    }
}
