using System.Threading;
using System.Threading.Tasks;

namespace theatrel.Interfaces.Cast
{
    public interface IPerformanceCastParser
    {
        Task<IPerformanceCast> Parse(string data, CancellationToken cancellationToken);
        Task<IPerformanceCast> ParseFromUrl(string url, bool wasMoved, CancellationToken cancellationToken);
    }
}
