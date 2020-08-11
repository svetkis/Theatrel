using System;
using System.Threading;
using System.Threading.Tasks;

namespace theatrel.Interfaces
{
    public interface IDataUpdater : IDIRegistrable
    {
        Task<bool> UpdateAsync(int theaterId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    }
}
