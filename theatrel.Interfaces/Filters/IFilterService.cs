using System;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.TgBot;

namespace theatrel.Interfaces.Filters;

public interface IFilterService : IDISingleton
{
    IPerformanceFilter GetFilter(IChatDataInfo dataInfo);
    IPerformanceFilter GetFilter(DateTime start, DateTime end);
    IPerformanceFilter GetFilter(int playbillEntryId);

    bool CheckOnlyDate(DateTime when, IPerformanceFilter filter);
    bool IsDataSuitable(string name, int locationId, string type, DateTime when, IPerformanceFilter filter);
    bool IsDataSuitable(int playbillEntryId, string name, int locationId, string type, DateTime when, IPerformanceFilter filter);
}