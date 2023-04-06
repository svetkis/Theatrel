using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Filters.Processors;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.TgBot;

namespace theatrel.Lib.Filters;

internal class FilterService : IFilterService
{
    private readonly IFilterProcessor[] _filterProcessors;
    private readonly IFilterProcessor _baseProcessors;

    public IDbService DbService { get; }

    public FilterService(IDbService dbService)
    {
        DbService = dbService;

        _filterProcessors = new IFilterProcessor[]
        {
            new ActorFilterProcessor(dbService),
            new PerformanceNameFilterProcessor(dbService),
            new PlaybillIdFilterProcessor(dbService)
        };

        _baseProcessors = new BaseFilterProcessor(dbService);
    }

    public IPerformanceFilter GetFilter(IChatDataInfo chatInfo)
    {
        if (!string.IsNullOrEmpty(chatInfo.PerformanceName))
        {
            return new PerformanceFilter
            {
                PerformanceName = chatInfo.PerformanceName,
                LocationIds = chatInfo.LocationIds,
                TheatreIds = chatInfo.TheatreIds,
            };
        }

        if (!string.IsNullOrEmpty(chatInfo.Actor))
        {
            return new PerformanceFilter
            {
                Actor = chatInfo.Actor
            };
        }

        var filter = new PerformanceFilter
        {
            StartDate = chatInfo.When,
            EndDate = chatInfo.When.AddMonths(1)
        };

        if (chatInfo.TheatreIds != null && chatInfo.TheatreIds.Any())
            filter.TheatreIds = chatInfo.TheatreIds;

        if (chatInfo.LocationIds != null && chatInfo.LocationIds.Any())
            filter.LocationIds = chatInfo.LocationIds;

        if (chatInfo.Days != null && chatInfo.Days.Any())
        {
            var days = chatInfo.Days.Distinct().ToArray();
            if (days.Length < 7)
                filter.DaysOfWeek = days.ToArray();
        }

        if (chatInfo.Types != null && chatInfo.Types.Any())
            filter.PerformanceTypes = chatInfo.Types;

        return filter;
    }

    public IPerformanceFilter GetOneMonthFilter(DateTime start) =>
        new PerformanceFilter
        {
            StartDate = start,
            EndDate = start.AddMonths(1)
        };

    public IPerformanceFilter GetFilter(int playbillEntryId) =>
        new PerformanceFilter
        {
            PlaybillId = playbillEntryId
        };

    public PlaybillChangeEntity[] GetFilteredChanges(PlaybillChangeEntity[] changes, SubscriptionEntity subscription)
    {
        var filter = subscription.PerformanceFilter;
        var processor = _filterProcessors.FirstOrDefault(x => x.IsCorrectProcessor(filter)) ?? _baseProcessors;

        return changes
            .Where(x => x.LastUpdate > subscription.LastUpdate && (subscription.TrackingChanges & x.ReasonOfChanges) != 0)
            .Where(change => processor.IsChangeSuitable(change, filter))
            .ToArray();
    }

    public PlaybillChangeEntity[] GetVkFilteredChanges(PlaybillChangeEntity[] changes, VkSubscriptionEntity subscription)
    {
        var filter = subscription.PerformanceFilter;
        var processor = _filterProcessors.FirstOrDefault(x => x.IsCorrectProcessor(filter)) ?? _baseProcessors;

        return changes
            .Where(x => x.LastUpdate > subscription.LastUpdate && (subscription.TrackingChanges & x.ReasonOfChanges) != 0)
            .Where(change => processor.IsChangeSuitable(change, filter))
            .ToArray();
    }

    public PlaybillEntity[] GetFilteredPerformances(IPerformanceFilter filter)
    {
        var processor = _filterProcessors.FirstOrDefault(x => x.IsCorrectProcessor(filter)) ?? _baseProcessors;

        return processor
            .GetFilteredPerformances(filter)
            .Where(item =>
            {
                if (item.When < DateTime.UtcNow)
                    return false;

                if (!item.Changes.Any())
                    return false;

                var lastChange = item.Changes.OrderBy(ch => ch.LastUpdate).Last();
                if (lastChange.ReasonOfChanges == (int)ReasonOfChanges.WasMoved)
                    return false;

                return true;
            })
            .ToArray();
    }
}