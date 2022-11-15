using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Filters;

namespace theatrel.Filters.Processors;

internal class PlaybillIdFilterProcessor : BaseFilterProcessor
{
    public PlaybillIdFilterProcessor(IDbService dbService) : base(dbService)
    {
    }

    public override bool IsCorrectProcessor(IPerformanceFilter filter)
    {
        return filter.PlaybillId != -1;
    }

    public override bool IsPerfomanceSuitable(PlaybillEntity playbillEntity, IPerformanceFilter filter)
    {
        if (filter.PlaybillId != playbillEntity.Id)
            return false;

        return CheckWasMoved(playbillEntity);
    }
}
