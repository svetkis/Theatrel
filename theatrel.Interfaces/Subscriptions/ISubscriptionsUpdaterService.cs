using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Subscriptions;

public interface ISubscriptionsUpdaterService : IDIRegistrable
{
    Task<bool> CleanUpOutDatedSubscriptions();
    Task<bool> ProlongSubscriptions();
}