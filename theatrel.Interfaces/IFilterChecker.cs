namespace theatrel.Interfaces
{
    public interface IFilterChecker : IDISingleton
    {
        bool IsDataSuitable(IPerformanceData performance, IPerformanceFilter filter);
    }
}
