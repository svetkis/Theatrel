using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Filters;

namespace theatrel.Interfaces.Subscriptions;

public interface ISubscriptionService : IDIRegistrable
{
    public IPerformanceFilter[] GetUpdateFilters();
}