using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.DataUpdater;

namespace theatrel.DataUpdater;

internal class PlaybillCleanUpService : IPlaybillCleanUpService
{
    private readonly IDbService _dbService;
    public PlaybillCleanUpService(IDbService dbService)
    {
        _dbService = dbService;
    }

    public async Task<bool> CleanUp()
    {
        using IPlaybillRepository repo = _dbService.GetPlaybillRepository();

        var oldPlaybillEntities = repo.GetOutdatedList();
        bool result = true;
        foreach (var entity in oldPlaybillEntities)
        {
            if (await repo.Delete(entity))
                result = false;
        }

        var oldPerformanceEntities = repo.GetOutdatedPerformanceEntities();
        foreach (var entity in oldPerformanceEntities)
        {
            if (await repo.Delete(entity))
                result = false;
        }

        return result;
    }
}