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
        using ISubscriptionsRepository subscriptionRepository = _dbService.GetSubscriptionRepository();
        string[] prolongFor = Environment.GetEnvironmentVariable("AutoProlongFullSubscriptionsUsers")?.Split(";").ToArray();
        string prolongMonthsString = Environment.GetEnvironmentVariable("AutoProlongFullSubscriptionsMonths");
        if (null == prolongFor)
            return true;

        int prolongMonths = int.TryParse(prolongMonthsString, out int outInt) ? outInt : 1;

        int trackIt = (int)(ReasonOfChanges.StartSales | ReasonOfChanges.Creation |
                    ReasonOfChanges.PriceDecreased | ReasonOfChanges.CastWasChanged |
                    ReasonOfChanges.CastWasSet | ReasonOfChanges.WasMoved);

        int currMonth = DateTime.Now.Month;
        int currYear = DateTime.Now.Year;

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

                SubscriptionEntity subscription = await subscriptionRepository.Create(
                        userId,
                        trackIt,
                        _filterService.GetFilter(startDate, startDate.AddMonths(1)), cancellationToken);
            }
        }

        return true;
    }
}