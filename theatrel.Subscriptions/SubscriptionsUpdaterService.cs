using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;

namespace theatrel.Subscriptions;

internal class SubscriptionsUpdaterService : ISubscriptionsUpdaterService
{
    private readonly IDbService _dbService;
    private readonly IFilterService _filterService;

    public SubscriptionsUpdaterService(IDbService dbService, IFilterService filterService)
    {
        _dbService = dbService;
        _filterService = filterService;
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

    public async Task<bool> ProlongSubscriptions(CancellationToken cancellationToken)
    {
        string[] prolongFor;
        string prolongMonthsString;

        try
        {
            string prolongForString = Environment.GetEnvironmentVariable("AutoProlongFullSubscriptionsUsers");
            prolongFor = prolongForString.Split(";", StringSplitOptions.RemoveEmptyEntries).ToArray();
            prolongMonthsString = Environment.GetEnvironmentVariable("AutoProlongFullSubscriptionsMonths");

            if (null == prolongFor || !prolongFor.Any())
                return true;
        }
        catch(Exception)
        {
            return true;
        }

        using ISubscriptionsRepository subscriptionRepository = _dbService.GetSubscriptionRepository();

        int prolongMonths = int.TryParse(prolongMonthsString, out int outInt) ? outInt : 1;

        int trackIt = (int)(ReasonOfChanges.StartSales | ReasonOfChanges.Creation |
                    ReasonOfChanges.PriceDecreased | ReasonOfChanges.CastWasChanged |
                    ReasonOfChanges.CastWasSet | ReasonOfChanges.WasMoved);

        foreach (string user in prolongFor)
        {
            long userId = long.Parse(user);

            var existSubscriptions = subscriptionRepository.GetUserSubscriptions(userId);
            for(int addMonth = 0; addMonth < prolongMonths; ++addMonth)
            {
                var currDt = DateTime.Now.AddMonths(addMonth);
                DateTime startDate = new DateTime(currDt.Year, currDt.Month, 1);

                var existSubscription = existSubscriptions.FirstOrDefault(x =>
                {
                    return x.TrackingChanges == trackIt &&
                            x.PerformanceFilter.StartDate.Month == startDate.Month &&
                            x.PerformanceFilter.StartDate.Year == startDate.Year;
                });
                
                if (null != existSubscription)
                    continue;

                await subscriptionRepository.Create(
                    userId,
                    trackIt,
                    _filterService.GetOneMonthFilter(startDate), cancellationToken);
            }
        }

        return true;
    }
}