using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Subscriptions;

namespace theatrel.Subscriptions;

internal class SubscriptionsUpdaterService : ISubscriptionsUpdaterService
{
    private readonly IDbService _dbService;
    public SubscriptionsUpdaterService(IDbService dbService)
    {
        _dbService = dbService;
    }

    public async Task<bool> CleanUpOutDatedSubscriptions()
    {
        using ISubscriptionsRepository repo = _dbService.GetSubscriptionRepository();

        IEnumerable<SubscriptionEntity> oldEntities = repo.GetOutdatedList().Distinct().ToArray();
        bool result = true;
        foreach (SubscriptionEntity entity in oldEntities)
        {
            if (!await repo.Delete(entity))
                result = false;
        }

        return result;
    }

    public Task<bool> ProlongSubscriptions()
    {
        using ISubscriptionsRepository repo = _dbService.GetSubscriptionRepository();

        return repo.ProlongSubscriptions();
    }
}