using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.TgBot;

namespace theatrel.Interfaces.Filters;

public interface IFilterService : IDIRegistrable
{
    IPerformanceFilter GetFilter(IChatDataInfo chatInfo);
    IPerformanceFilter GetFilter(DateTime start, DateTime end);
    IPerformanceFilter GetFilter(int playbillEntryId);

    public PlaybillEntity[] GetFilteredPerformances(IPerformanceFilter filter);
    PlaybillChangeEntity[] GetFilteredChanges(PlaybillChangeEntity[] changes, SubscriptionEntity subscription);
}