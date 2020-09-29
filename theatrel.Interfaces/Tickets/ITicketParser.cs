using System.Threading;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Tickets
{
    public interface ITicketParser : IDIRegistrable
    {
        ITicket Parse(object ticket, CancellationToken cancellationToken);
    }
}
