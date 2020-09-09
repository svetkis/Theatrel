using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.DataUpdater;

namespace theatrel.DataUpdater
{
    internal class PlaybillCleanUpService : IPlaybillCleanUpService
    {
        private readonly IDbService _dbService;
        public PlaybillCleanUpService(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<bool> CleanUp()
        {
            using var repo = _dbService.GetPlaybillRepository();

            var oldPlaybillEntities = repo.GetOutdatedList();
            bool result = true;
            foreach (var entity in oldPlaybillEntities)
            {
                if (await repo.Delete(entity))
                    result = false;
            }

            return result;
        }

        public void Dispose()
        {
        }
    }
}
