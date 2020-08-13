using System;

namespace theatrel.Interfaces
{
    public interface IFilterHelper : IDISingleton
    {
        IPerformanceFilter GetFilter(IChatDataInfo dataInfo);
        IPerformanceFilter GetFilter(DateTime start, DateTime end);
    }
}
