using System;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Playbill;

namespace theatrel.Interfaces.Filters
{
    public interface IFilterChecker : IDISingleton
    {
        bool IsDataSuitable(IPerformanceData performance, IPerformanceFilter filter);
        bool IsDataSuitable(string location, string type, DateTime when, IPerformanceFilter filter);
    }
}
