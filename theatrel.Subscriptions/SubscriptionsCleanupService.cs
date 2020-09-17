using System.Collections.Generic;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Subscriptions;

namespace theatrel.Subscriptions
{
    internal class SubscriptionsCleanupService : ISubscriptionsCleanupService
    {
        private readonly IDbService _dbService;
        public SubscriptionsCleanupService(IDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<bool> CleanUp()
        {
            using ISubscriptionsRepository repo = _dbService.GetSubscriptionRepository();

            IEnumerable<SubscriptionEntity> oldEntities = repo.GetOutdatedList();
            bool result = true;
            foreach (SubscriptionEntity entity in oldEntities)
            {
                if (await repo.Delete(entity))
                    result = false;
            }

            return result;
        }
    }
}
