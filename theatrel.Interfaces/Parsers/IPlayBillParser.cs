using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Playbill;

namespace theatrel.Interfaces.Parsers;

public interface IPlaybillParser
{
    Task<IPerformanceData[]> Parse(byte[] playbill, IPerformanceParser performanceParser, int year, int month, CancellationToken cancellationToken);
}