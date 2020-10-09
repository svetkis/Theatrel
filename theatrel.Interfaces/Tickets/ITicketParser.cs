using System.Threading;

namespace theatrel.Interfaces.Tickets
{
    public interface ITicketParser
    {
        ITicket Parse(object ticket, CancellationToken cancellationToken);
    }
}
