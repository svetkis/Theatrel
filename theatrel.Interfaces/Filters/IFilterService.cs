using System;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.TgBot;

namespace theatrel.Interfaces.Filters
{
    public interface IFilterService : IDISingleton
    {
        IPerformanceFilter GetFilter(IChatDataInfo dataInfo);
        IPerformanceFilter GetFilter(DateTime start, DateTime end);
    }
}
