namespace theatrel.Interfaces
{
    public interface ISubscriptionService : IDISingleton
    {
        public IPerformanceFilter[] GetUpdateFilters();
    }
}
