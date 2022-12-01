using System.Threading;
using System.Threading.Tasks;

namespace theatrel.Interfaces.Cast;

public interface IPerformanceCastParser
{
    Task<IPerformanceCast> ParseFromUrl(string url, string castFromPlaybill, bool wasMoved, CancellationToken cancellationToken);
}