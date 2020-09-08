using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;

namespace theatrel.DataUpdater
{
    internal class DbPlaybillUpdater : IDbPlaybillUpdater
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
            Trace.TraceInformation(" PlaybillUpdater started.");

            using var dbRepository = _dbService.GetPlaybillRepository();

            IPerformanceData[] performances = await _dataResolver.RequestProcess(_filterService.GetFilter(startDate, endDate), cancellationToken);
            foreach (var freshPerformanceData in performances)
            {
                await ProcessData(freshPerformanceData, dbRepository);
            }

            Trace.TraceInformation("PlaybillUpdater finished.");
            return true;
        }

        private async Task ProcessData(IPerformanceData data, IPlaybillRepository playbillRepository)
        {
            if (data.DateTime < DateTime.Now)
                return;

            var playbillEntry = playbillRepository.Get(data);
            if (null == playbillEntry)
            {
                await playbillRepository.AddPlaybill(data);
                return;
            }

            var lastChange = playbillEntry.Changes
                .OrderByDescending(x => x.LastUpdate)
                .FirstOrDefault();

            var compareResult = ComparePerformanceData(lastChange, data);
            if (compareResult == ReasonOfChanges.NoReason && lastChange != null)
            {
                lastChange.LastUpdate = DateTime.Now;
                await playbillRepository.Update(lastChange);
                return;
            }

            Trace.TraceInformation($"{data.Name} {data.DateTime:g} was changed. MinPrice is {data.MinPrice}");

            await playbillRepository.AddChange(playbillEntry, new PlaybillChangeEntity
            {
                LastUpdate = DateTime.Now,
                MinPrice = data.MinPrice,
                ReasonOfChanges = (int)compareResult,
            });
        }

        private ReasonOfChanges ComparePerformanceData(PlaybillChangeEntity lastChange, IPerformanceData freshData)
        {
            int freshMinPrice = freshData.MinPrice;

            if (lastChange == null)
                return freshMinPrice == 0 ? ReasonOfChanges.Creation : ReasonOfChanges.StartSales;

            if (lastChange.MinPrice == 0)
                return freshMinPrice == 0 ? ReasonOfChanges.NoReason : ReasonOfChanges.StartSales;

            if (lastChange.MinPrice > freshMinPrice)
            {
                // we skip cases when price was not zero, and then becomes zero because it some technical
                // things and it produces a lot of DB records without useful information
                return freshMinPrice != 0 ? ReasonOfChanges.PriceDecreased : ReasonOfChanges.NoReason;
            }

            if (lastChange.MinPrice < freshMinPrice)
                return ReasonOfChanges.PriceIncreased;

            return ReasonOfChanges.NoReason;
        }
    }
}
