﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using theatrel.Common.Enums;

namespace theatrel.DataUpdater
{
    public class DataUpdater : IDataUpdater
    {
        private readonly IPlayBillDataResolver _dataResolver;
        private readonly AppDbContext _dbContext;
        private readonly IFilterHelper _filterHelper;

        public DataUpdater(IPlayBillDataResolver dataResolver, AppDbContext dbContext, IFilterHelper filterHelper)
        {
            _dataResolver = dataResolver;
            _dbContext = dbContext;
            _filterHelper = filterHelper;
        }

        public async Task<bool> UpdateAsync(int theaterId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("DataUpdater.UpdateAsync started.");
            PerformanceEntity[] savedPerformances =
                _dbContext.Performances
                    .Include(x => x.Changes)
                    .Where(p => p.DateTime >= startDate && p.DateTime <= endDate)
                    .ToArray();

            IPerformanceData[] performances = await _dataResolver.RequestProcess(_filterHelper.GetFilter(startDate, endDate), cancellationToken);
            foreach (var freshPerformanceData in performances)
            {
                Trace.TraceInformation($"Process {freshPerformanceData.Name}");
                var savedPerformance = savedPerformances.FirstOrDefault(p =>
                    string.Compare(p.Url, freshPerformanceData.Url, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (savedPerformance == null)
                {
                    Trace.TraceInformation($"Performance {freshPerformanceData.Name} will be added to database");
                    _dbContext.Performances.Add(CreatePerformanceEntity(freshPerformanceData));
                    continue;
                }

                Trace.TraceInformation($"PerformanceChanges {freshPerformanceData.Name} will be changed");

                if (savedPerformance.Changes == null)
                    Trace.TraceInformation($"Performance {savedPerformance.Name} has no changes");

                PerformanceChangeEntity lastChange = savedPerformance.Changes?.OrderByDescending(x => x.LastUpdate)
                    .FirstOrDefault();

                var compareResult = ComparePerformanceData(lastChange, freshPerformanceData);
                if (compareResult == ReasonOfChanges.NoReason && lastChange != null)
                {
                    Trace.TraceInformation("Just update LastUpdate.");
                    lastChange.LastUpdate = DateTime.Now;
                    continue;
                }

                savedPerformance.Changes?.Add(CreatePerformanceChangeEntity(freshPerformanceData.MinPrice, compareResult));
                Trace.TraceInformation($"Performance change information was added {compareResult}");
            }

            Trace.TraceInformation("Save Changes to db");
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        private PerformanceChangeEntity CreatePerformanceChangeEntity(int minPrice, ReasonOfChanges reason)
        {
            return new PerformanceChangeEntity
            {
                LastUpdate = DateTime.Now,
                MinPrice = minPrice,
                ReasonOfChanges = (int)reason,
            };
        }

        private PerformanceEntity CreatePerformanceEntity(IPerformanceData data) =>
            new PerformanceEntity
            {
                Name = data.Name,
                Location = data.Location,
                Type = data.Type,
                DateTime = data.DateTime,
                Url = data.Url,
                Changes = new List<PerformanceChangeEntity>
                {
                    new PerformanceChangeEntity
                    {
                        LastUpdate = DateTime.Now,
                        MinPrice = data.MinPrice,
                        ReasonOfChanges = (int) ReasonOfChanges.Creation
                    }
                }
            };

        private ReasonOfChanges ComparePerformanceData(PerformanceChangeEntity lastChange, IPerformanceData freshData)
        {
            int freshMinPrice = freshData.MinPrice;

            if (lastChange == null)
                return freshMinPrice == 0 ? ReasonOfChanges.Creation : ReasonOfChanges.StartSales;

            if (lastChange.MinPrice == 0)
                return freshMinPrice == 0 ? ReasonOfChanges.NoReason : ReasonOfChanges.StartSales;

            if (lastChange.MinPrice > freshMinPrice)
                return ReasonOfChanges.PriceDecreased;

            if (lastChange.MinPrice < freshMinPrice)
                return ReasonOfChanges.PriceIncreased;

            return ReasonOfChanges.NoReason;
        }
    }
}
