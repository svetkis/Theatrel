using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Filters;

namespace theatrel.Filters.Processors;

internal class PerformanceNameFilterProcessor : BaseFilterProcessor
{
    public PerformanceNameFilterProcessor(IDbService dbService) : base(dbService)
    {
    }

    public override bool IsCorrectProcessor(IPerformanceFilter filter)
    {
        return !string.IsNullOrEmpty(filter.PerformanceName);
    }

    public override bool IsPerfomanceSuitable(PlaybillEntity playbillEntity, IPerformanceFilter filter)
    {
        if (!playbillEntity.Performance.Name.ToLower().Contains(filter.PerformanceName.ToLower()))
            return false;

        if (!CheckLocation(filter.LocationIds, playbillEntity.Performance.LocationId))
            return false;

        return CheckWasMoved(playbillEntity);
    }

    protected override PlaybillEntity[] GetData(IPerformanceFilter filter)
    {
        using var playbillRepo = DbService.GetPlaybillRepository();
        return playbillRepo.GetListByName(filter.PerformanceName).ToArray();
    }
}
