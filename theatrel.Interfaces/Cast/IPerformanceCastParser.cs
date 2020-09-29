using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Cast
{
    public interface IPerformanceCastParser : IDIRegistrable
    {
        Task<IPerformanceCast> Parse(string data, CancellationToken cancellationToken);
        Task<IPerformanceCast> ParseFromUrl(string url, CancellationToken cancellationToken);
    }
}
