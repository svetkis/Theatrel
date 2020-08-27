using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Filters;

namespace theatrel.Interfaces.Playbill
{
    public interface IPlayBillDataResolver : IDIRegistrable
    {
        Task<IPerformanceData[]> RequestProcess(IPerformanceFilter filter, CancellationToken cancellationToken);
    }
}
