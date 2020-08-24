using System;
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

            IPerformanceData[] performances = await _dataResolver.RequestProcess(_filterHelper.GetFilter(startDate, endDate), cancellationToken);
            foreach (var freshPerformanceData in performances)
            {
                Trace.TraceInformation($"Process {freshPerformanceData.Name} {freshPerformanceData.DateTime:g}");
                await ProcessData(freshPerformanceData);
            }

            Trace.TraceInformation("Save Changes to db");
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task ProcessData(IPerformanceData data)
        {
            var location = _dbContext.PerformanceLocations.FirstOrDefault(l => l.Name == data.Location);
            var type = _dbContext.PerformanceTypes.FirstOrDefault(l => l.TypeName == data.Type);
            var performance = location != null && type != null
                ? _dbContext.Performances.FirstOrDefault(p => p.Location == location && p.Type == type && p.Name == data.Name)
                : null;

            if (performance == null)
            {
                Trace.TraceInformation($"Performance not found");

                performance = new PerformanceEntity
                {
                    Name = data.Name,
                    Location = location ?? new LocationsEntity {Name = data.Location},
                    Type = type ?? new PerformanceTypeEntity {TypeName = data.Type},
                };

                _dbContext.Performances.Add(performance);

                AddNewPlaybillEntry(performance, data);

                await _dbContext.SaveChangesAsync();
                return;
            }

            var existPlaybillEntry = _dbContext.Playbill
                .Include(x => x.Changes)
                .FirstOrDefault(x => x.When == data.DateTime && x.Performance == performance);

            if (existPlaybillEntry == null)
            {
                Trace.TraceInformation($"Performance found, playbill entry not found");
                AddNewPlaybillEntry(performance, data);
                return;
            }

            Trace.TraceInformation($"Performance found, playbill found, need to update change");
            var lastChange = existPlaybillEntry.Changes
                .OrderByDescending(x => x.LastUpdate)
                .FirstOrDefault();

            var compareResult = ComparePerformanceData(lastChange, data);
            if (compareResult == ReasonOfChanges.NoReason && lastChange != null)
            {
                Trace.TraceInformation("Just update LastUpdate.");
                lastChange.LastUpdate = DateTime.Now;
                return;
            }

            var newChange = new PlaybillChangeEntity
            {
                LastUpdate = DateTime.Now,
                MinPrice = data.MinPrice,
                ReasonOfChanges = (int)compareResult,
            };

            existPlaybillEntry.Changes.Add(newChange);
            _dbContext.Add(newChange);
        }

        private void AddNewPlaybillEntry(PerformanceEntity performance, IPerformanceData data)
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
                Changes = new List<PlaybillChangeEntity>{change}
            };

            _dbContext.Playbill.Add(playBillEntry);
            _dbContext.Add(change);
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
