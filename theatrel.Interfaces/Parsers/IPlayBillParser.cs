using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Playbill;

namespace theatrel.Interfaces.Parsers
{
    public interface IPlaybillParser : IDIRegistrable
    {
        Task<IPerformanceData[]> Parse(string playbill, CancellationToken cancellationToken);
    }
}
