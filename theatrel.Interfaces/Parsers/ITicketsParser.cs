using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Playbill;

namespace theatrel.Interfaces.Parsers
{
    public interface ITicketsParser : IDIRegistrable
    {
        Task<IPerformanceTickets> Parse(string data, CancellationToken cancellationToken);
        Task<IPerformanceTickets> ParseFromUrl(string url, CancellationToken cancellationToken);
    }
}
