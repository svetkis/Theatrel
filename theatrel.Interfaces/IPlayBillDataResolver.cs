using System;
using System.Threading;
using System.Threading.Tasks;

namespace theatrel.Interfaces
{
    public interface IPlayBillDataResolver : IDIRegistrable
    {
        Task<IPerformanceData[]> RequestProcess(IPerformanceFilter filter, CancellationToken cancellationToken);
    }
}
