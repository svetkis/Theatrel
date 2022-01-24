using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Tickets;

public interface ITicketsParser : IDIRegistrable
{
    Task<IPerformanceTickets> Parse(byte[] data, CancellationToken cancellationToken);
    Task<IPerformanceTickets> ParseFromUrl(string url, CancellationToken cancellationToken);
}