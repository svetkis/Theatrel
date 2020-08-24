using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using theatrel.Lib;

namespace theatrel.Subscriptions
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly AppDbContext _dbContext;

        public SubscriptionService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IPerformanceFilter[] GetUpdateFilters()
        {
            if (!_dbContext.Subscriptions.Any())
                return null;

            List<IPerformanceFilter> mergedFilters = new List<IPerformanceFilter>();
            foreach (var subscription in _dbContext.Subscriptions.Local)
            {
                PerformanceFilterEntity newFilter = subscription.PerformanceFilter;

                if (newFilter == null)
                    continue;

                int year;
                int month;
                DateTime startDate;
                DateTime endDate;
                if (newFilter.PerformanceId == -1)
                {
                    year = newFilter.StartDate.Year;
                    month = newFilter.StartDate.Month;
                    startDate = new DateTime(year, month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                }
                else
                {
                    var playbillEntry = _dbContext.Playbill.AsNoTracking()
                        .FirstOrDefault(entity => entity.Id == newFilter.PerformanceId);

                    if (null == playbillEntry)
                        continue;

                    year = playbillEntry.When.Year;
                    month = playbillEntry.When.Month;
                    startDate = new DateTime(year, month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                }

                MergeFilters(mergedFilters, new PerformanceFilter { StartDate = startDate, EndDate = endDate });
            }

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
