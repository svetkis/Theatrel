using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Filters;

namespace theatrel.Filters.Processors;

internal class ActorFilterProcessor : BaseFilterProcessor
{
    private readonly Dictionary<string, string[]> _filterToActorNames = new ();
    public ActorFilterProcessor(IDbService dbService) : base(dbService)
    {
    }

    public override bool IsCorrectProcessor(IPerformanceFilter filter)
    {
        return !string.IsNullOrEmpty(filter.Actor);
    }

    public override bool IsPerfomanceSuitable(PlaybillEntity playbillEntity, IPerformanceFilter filter)
    {
        return CheckWasMoved(playbillEntity);
    }

    public override bool IsChangeSuitable(PlaybillChangeEntity change, IPerformanceFilter filter)
    {
        if (change.ReasonOfChanges != (int)ReasonOfChanges.CastWasChanged && change.ReasonOfChanges != (int)ReasonOfChanges.CastWasSet)
            return false;

        if (!_filterToActorNames.ContainsKey(filter.Actor))
        {
            using var playbillRepo = DbService.GetPlaybillRepository();
            
            _filterToActorNames.Add(
                filter.Actor,
                playbillRepo
                    .GetActorsByNameFilter(filter.Actor)
                    .Select(x => x.Name.ToLower())
                    .ToArray());
        }

        string[] filterActors = _filterToActorNames[filter.Actor];

        var addedList = change.CastAdded?.ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries).ToArray();
        var removedList = change.CastRemoved?.ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries).ToArray();

        bool added = addedList != null && addedList.Any(actor => filterActors.Contains(actor));
        bool removed = removedList != null && removedList.Any(actor => filterActors.Contains(actor));

        return added || removed;
    }

    protected override PlaybillEntity[] GetData(IPerformanceFilter filter)
    {
        using var playbillRepo = DbService.GetPlaybillRepository();

        return playbillRepo.GetListByActor(filter.Actor).ToArray();
    }
}
