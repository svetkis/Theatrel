using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;

namespace theatrel.DataUpdater
{
    public class DbPlaybillUpdater : IDbPlaybillUpdater
    {
        private readonly IPlayBillDataResolver _dataResolver;
        private readonly IDbService _dbService;
        private readonly IFilterService _filterService;

        public DbPlaybillUpdater(IPlayBillDataResolver dataResolver, IDbService dbService, IFilterService filterService)
        {
            _dataResolver = dataResolver;
            _dbService = dbService;
            _filterService = filterService;
        }

        public async Task<bool> UpdateAsync(int theaterId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("DbPlaybillUpdater update started.");

            await using var dbContext = _dbService.GetDbContext();

            IPerformanceData[] performances = await _dataResolver.RequestProcess(_filterService.GetFilter(startDate, endDate), cancellationToken);
            foreach (var freshPerformanceData in performances)
            {
                await ProcessData(freshPerformanceData, dbContext);
            }

            Trace.TraceInformation("DbPlaybillUpdater update finished.");
            return true;
        }

        private async Task ProcessData(IPerformanceData data, AppDbContext dbContext)
        {
            var location = dbContext.PerformanceLocations.FirstOrDefault(l => l.Name == data.Location);
            var type = dbContext.PerformanceTypes.FirstOrDefault(l => l.TypeName == data.Type);
            var performance = location != null && type != null
                ? dbContext.Performances.FirstOrDefault(p => p.Location == location && p.Type == type && p.Name == data.Name)
                : null;

            if (performance == null)
            {
                performance = new PerformanceEntity
                {
                    Name = data.Name,
                    Location = location ?? new LocationsEntity { Name = data.Location },
                    Type = type ?? new PerformanceTypeEntity { TypeName = data.Type },
                };

                dbContext.Performances.Add(performance);

                await AddNewPlaybillEntry(performance, data, dbContext);

                return;
            }

            var existPlaybillEntry = dbContext.Playbill
                .Include(x => x.Changes)
                .FirstOrDefault(x => x.When == data.DateTime && x.Performance == performance);

            if (existPlaybillEntry == null)
            {
                await AddNewPlaybillEntry(performance, data, dbContext);
                return;
            }

            var lastChange = existPlaybillEntry.Changes
                .OrderByDescending(x => x.LastUpdate)
                .FirstOrDefault();

            var compareResult = ComparePerformanceData(lastChange, data);
            if (compareResult == ReasonOfChanges.NoReason && lastChange != null)
            {
                lastChange.LastUpdate = DateTime.Now;
                await dbContext.SaveChangesAsync();

                return;
            }

            var newChange = new PlaybillChangeEntity
            {
                LastUpdate = DateTime.Now,
                MinPrice = data.MinPrice,
                ReasonOfChanges = (int)compareResult,
            };

            Trace.TraceInformation($"{data.Name} {data.DateTime:g} was changed. MinPrice is {data.MinPrice}");
            existPlaybillEntry.Changes.Add(newChange);
            dbContext.Add(newChange);
            await dbContext.SaveChangesAsync();
        }

        private async Task AddNewPlaybillEntry(PerformanceEntity performance, IPerformanceData data, AppDbContext dbContext)
        {
            var change = new PlaybillChangeEntity
            {
                LastUpdate = DateTime.Now,
                MinPrice = data.MinPrice,
                ReasonOfChanges = (int)ReasonOfChanges.Creation,
            };

            var playBillEntry = new PlaybillEntity
            {
                Performance = performance,
                Url = data.Url,
                When = data.DateTime,
                Changes = new List<PlaybillChangeEntity> { change }
            };

            dbContext.Playbill.Add(playBillEntry);
            dbContext.Add(change);

            await dbContext.SaveChangesAsync();
        }

        private ReasonOfChanges ComparePerformanceData(PlaybillChangeEntity lastChange, IPerformanceData freshData)
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
