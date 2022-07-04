using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;

namespace theatrel.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
    private readonly IDbService _dbService;
    private readonly IFilterService _filterService;

    public SubscriptionService(IDbService dbService, IFilterService filterService)
    {
        _dbService = dbService;
        _filterService = filterService;
    }

    public IPerformanceFilter[] GetUpdateFilters()
    {
        using var subscriptionRepository = _dbService.GetSubscriptionRepository();
        using var playbillRepository = _dbService.GetPlaybillRepository();

        var subscriptions = subscriptionRepository.GetAllWithFilter().ToArray();
        if (!subscriptions.Any())
            return Array.Empty<IPerformanceFilter>();

        List<IPerformanceFilter> mergedFilters = new List<IPerformanceFilter>();
        foreach (var newFilter in subscriptions.Select(item => item.PerformanceFilter))
        {
            if (newFilter == null)
                continue;

            if (!string.IsNullOrEmpty(newFilter.PerformanceName))
                continue;

            DateTime startDate;
            DateTime endDate;

            if (newFilter.PlaybillId == -1)
            {
                startDate = newFilter.StartDate;
                endDate = newFilter.EndDate;
            }
            else
            {
                var playbillEntry = playbillRepository.GetPlaybill(newFilter.PlaybillId);

                if (null == playbillEntry)
                    continue;

                int year = playbillEntry.When.Year;
                int month = playbillEntry.When.Month;
                startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.AddMonths(1);
            }

            MergeFilters(mergedFilters, _filterService.GetFilter(startDate, endDate));
        }

        Trace.TraceInformation("Get updated filter finished");
        return mergedFilters.ToArray();
    }

    // first was coded the simplest merge
    private void MergeFilters(List<IPerformanceFilter> filters, IPerformanceFilter newFilter)
    {
        if (!filters.Any(filter =>
                filter.StartDate.Year == newFilter.StartDate.Year &&
                filter.StartDate.Month == newFilter.StartDate.Month))
        {
            filters.Add(newFilter);
        }
    }
}