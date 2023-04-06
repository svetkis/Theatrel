using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Subscriptions;

public interface ISubscriptionsUpdaterService : IDIRegistrable
{
    Task<bool> CleanUpOutDatedSubscriptions();
    Task<bool> ProlongSubscriptions(CancellationToken cancellationToken);
    Task<bool> ProlongSubscriptionsVk(CancellationToken cancellationToken);
}