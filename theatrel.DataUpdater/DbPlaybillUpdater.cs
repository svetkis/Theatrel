using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using theatrel.Lib;

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
            Trace.TraceInformation("PlaybillUpdater started.");

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

            if ((string.IsNullOrEmpty(playbillEntry.TicketsUrl) || string.Equals(playbillEntry.TicketsUrl, CommonTags.NotDefined, StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrEmpty(data.TicketsUrl) && !string.Equals(data.TicketsUrl, CommonTags.NotDefined, StringComparison.OrdinalIgnoreCase))
            {
                await playbillRepository.UpdateTicketsUrl(playbillEntry.Id, data.TicketsUrl);
            }

            if ((string.IsNullOrEmpty(playbillEntry.Url) || string.Equals(playbillEntry.Url, CommonTags.NotDefined, StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrEmpty(data.Url) && !string.Equals(data.Url, CommonTags.NotDefined, StringComparison.OrdinalIgnoreCase))
            {
                await playbillRepository.UpdateUrl(playbillEntry.Id, data.Url);
            }

            var compareResult = ComparePerformanceData(lastChange, data);
            if (compareResult == ReasonOfChanges.NothingChanged
                && lastChange != null && lastChange.ReasonOfChanges == (int)ReasonOfChanges.NothingChanged)
            {
                return;
            }

            if (compareResult == ReasonOfChanges.DataError)
            {
                Trace.TraceInformation($"Data error {data.Name} {data.DateTime:M} price: {data.MinPrice}");
                return;
            }

            if (compareResult != ReasonOfChanges.NothingChanged)
                Trace.TraceInformation($"Reason of changes {compareResult} {data.Name} {data.DateTime:M} price: {data.MinPrice}");

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

            if (freshMinPrice == -1)
                return ReasonOfChanges.DataError;

            if (lastChange == null)
                return freshMinPrice == 0 ? ReasonOfChanges.Creation : ReasonOfChanges.StartSales;

            if (lastChange.MinPrice == 0)
                return freshMinPrice == 0 ? ReasonOfChanges.NothingChanged : ReasonOfChanges.StartSales;

            if (lastChange.MinPrice > freshMinPrice)
                return freshMinPrice != 0 ? ReasonOfChanges.PriceDecreased : ReasonOfChanges.StopSales;

            if (lastChange.MinPrice < freshMinPrice)
                return ReasonOfChanges.PriceIncreased;

            return ReasonOfChanges.NothingChanged;
        }

        public void Dispose()
        {
        }
    }
}
