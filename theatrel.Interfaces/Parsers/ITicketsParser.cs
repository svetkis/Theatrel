using System.Threading;
using System.Threading.Tasks;

namespace theatrel.Interfaces.Parsers
{
    public interface ITicketsParser : IDIRegistrable
    {
        Task<IPerformanceTickets> Parse(string data, CancellationToken cancellationToken);
        Task<IPerformanceTickets> ParseFromUrl(string url, CancellationToken cancellationToken);
    }
}
