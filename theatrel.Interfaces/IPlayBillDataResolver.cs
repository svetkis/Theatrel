using System;
using System.Threading.Tasks;

namespace theatrel.Interfaces
{
    public interface IPlayBillDataResolver : IDIRegistrable
    {
        Task<IPerformanceData[]> RequestProcess(DateTime startDate, DateTime endDate, IPerformanceFilter filter);
    }
}
