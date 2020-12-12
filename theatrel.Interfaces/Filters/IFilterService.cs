using System;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Playbill;
using theatrel.Interfaces.TgBot;

namespace theatrel.Interfaces.Filters
{
    public interface IFilterService : IDISingleton
    {
        IPerformanceFilter GetFilter(IChatDataInfo dataInfo);
        IPerformanceFilter GetFilter(DateTime start, DateTime end);
        IPerformanceFilter GetFilter(int playbillEntryId);

        bool IsDataSuitable(IPerformanceData performance, IPerformanceFilter filter);
        bool IsDataSuitable(int playbillEntryId, string name, string location, string type, DateTime when, IPerformanceFilter filter);
    }
}
