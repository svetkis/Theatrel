using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.DataUpdater;

public interface IDbPlaybillUpdater : IDIRegistrable
{
    Task<bool> Update(int theaterId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
}