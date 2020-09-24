using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;

namespace theatrel.Subscriptions
{
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
                return new IPerformanceFilter[0];

            List<IPerformanceFilter> mergedFilters = new List<IPerformanceFilter>();
            foreach (var subscription in subscriptions)
            {
                PerformanceFilterEntity newFilter = subscription.PerformanceFilter;

                if (newFilter == null)
                    continue;

                if (!string.IsNullOrEmpty(newFilter.PerformanceName))
                    continue;

                int year;
                int month;
                DateTime startDate;
                DateTime endDate;

                if (newFilter.PlaybillId == -1)
                {
                    year = newFilter.StartDate.Year;
                    month = newFilter.StartDate.Month;
                    startDate = new DateTime(year, month, 1);
                    endDate = new DateTime(newFilter.EndDate.Year, newFilter.EndDate.Month, 1).AddMonths(1);
                }
                else
                {
                    var playbillEntry = playbillRepository.Get(newFilter.PlaybillId);

                    if (null == playbillEntry)
                        continue;

                    year = playbillEntry.When.Year;
                    month = playbillEntry.When.Month;
                    startDate = new DateTime(year, month, 1);
                    endDate = startDate.AddMonths(1);
                }

                MergeFilters(mergedFilters, _filterService.GetFilter(startDate, endDate));
            }

            Trace.TraceInformation("Get update filter finished");
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
}
