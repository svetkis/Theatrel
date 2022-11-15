using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Filters;
using theatrel.Lib.Filters;

namespace theatrel.Filters.Processors;

internal class BaseFilterProcessor : IFilterProcessor
{
    protected IDbService DbService { get; }

    public BaseFilterProcessor(IDbService dbService)
    {
        DbService = dbService;
    }

    protected virtual PlaybillEntity[] GetData(IPerformanceFilter filter)
    {
        using var playbillRepo = DbService.GetPlaybillRepository();
        return playbillRepo.GetList(filter.StartDate, filter.EndDate).ToArray();
    }

    public PlaybillEntity[] GetFilteredPerformances(IPerformanceFilter filter)
    {
        return GetData(filter)
            .Where(x => IsPerfomanceSuitable(x, filter))
            .ToArray();
    }

    public virtual bool IsCorrectProcessor(IPerformanceFilter filter) => false;

    public virtual bool IsPerfomanceSuitable(PlaybillEntity playbillEntity, IPerformanceFilter filter)
    {
        if (filter == null)
            return true;

        if (!CheckLocation(filter.LocationIds, playbillEntity.Performance.LocationId))
        {
            return false;
        }

        if (filter.PerformanceTypes != null && filter.PerformanceTypes.Any() && !string.IsNullOrEmpty(filter.PerformanceTypes.First())
            && filter.PerformanceTypes.All(val => 0 != string.Compare(val, playbillEntity.Performance.Type.TypeName, true)))
        {
            return false;
        }

        if (filter.DaysOfWeek != null && filter.DaysOfWeek.Any() && !filter.DaysOfWeek.Contains(playbillEntity.When.DayOfWeek))
        {
            return false;
        }

        if (filter.StartDate > playbillEntity.When || filter.EndDate < playbillEntity.When)
        {
            return false;
        }

        return CheckWasMoved(playbillEntity);
    }

    public virtual bool IsChangeSuitable(PlaybillChangeEntity change, IPerformanceFilter filter)
    {
        return IsPerfomanceSuitable(change.PlaybillEntity, filter);
    }

    protected bool CheckWasMoved(PlaybillEntity playbillEntity)
    {
        if (!playbillEntity.Changes.Any())
            return true;

        var lastChange = playbillEntity.Changes.OrderBy(ch => ch.LastUpdate).Last();
        return lastChange.ReasonOfChanges != (int)ReasonOfChanges.WasMoved;
    }

    protected static bool CheckLocation(int[] filterLocations, int locationId)
    {
        if (filterLocations == null || !filterLocations.Any())
            return true;

        return filterLocations.Contains(locationId);
    }
}
