using System;

namespace theatrel.Interfaces
{
    public interface IFilterChecker : IDISingleton
    {
        bool IsDataSuitable(IPerformanceData performance, IPerformanceFilter filter);
        bool IsDataSuitable(string location, string type, DateTime when, IPerformanceFilter filter);
    }
}
