namespace theatrel.Interfaces
{
    public interface IFilterChecker : IDISingleton
    {
        bool IsDataSuitable(IPerformanceData data, IPerformanceFilter filter);
    }
}
