using System.Threading;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Playbill;

namespace theatrel.Interfaces.Parsers
{
    public interface ITicketParser : IDIRegistrable
    {
        ITicket Parse(object ticket, CancellationToken cancellationToken);
    }
}
