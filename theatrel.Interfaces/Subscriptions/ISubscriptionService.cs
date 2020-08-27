using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Filters;

namespace theatrel.Interfaces.Subscriptions
{
    public interface ISubscriptionService : IDISingleton
    {
        public IPerformanceFilter[] GetUpdateFilters();
    }
}
