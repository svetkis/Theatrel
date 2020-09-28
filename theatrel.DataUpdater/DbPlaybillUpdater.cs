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

            if (!string.Equals(playbillEntry.TicketsUrl, data.TicketsUrl))
            {
                await playbillRepository.UpdateTicketsUrl(playbillEntry.Id, data.TicketsUrl);
            }

            if (!string.Equals(playbillEntry.Url, data.Url))
            {
                await playbillRepository.UpdateUrl(playbillEntry.Id, data.Url);
            }

            var compareResult = ComparePerformanceData(lastChange, data);
            switch (compareResult)
            {
                case ReasonOfChanges.NothingChanged:
                    return;
                case ReasonOfChanges.DataError:
                    Trace.TraceInformation($"Data error {data.Name} {data.DateTime:M} price: {data.MinPrice}");
                    return;
            }

            await playbillRepository.AddChange(playbillEntry, new PlaybillChangeEntity
            {
                LastUpdate = DateTime.Now,
                MinPrice = data.MinPrice,
                ReasonOfChanges = (int)compareResult,
            });
        }

        private ReasonOfChanges ComparePerformanceData(PlaybillChangeEntity lastChange, IPerformanceData freshData)
        {
            switch (freshData.State)
            {
                case TicketsState.TechnicalError:
                    return ReasonOfChanges.DataError;

                case TicketsState.PerformanceWasMoved:
                    return lastChange != null && lastChange.ReasonOfChanges == (int)ReasonOfChanges.WasMoved
                        ? ReasonOfChanges.NothingChanged
                        : ReasonOfChanges.WasMoved;

                case TicketsState.NoTickets:
                    return lastChange != null && lastChange.ReasonOfChanges == (int)ReasonOfChanges.StopSales
                        ? ReasonOfChanges.NothingChanged
                        : ReasonOfChanges.StopSales;

                case TicketsState.Ok:
                    if (lastChange == null)
                        return freshData.MinPrice == 0 ? ReasonOfChanges.Creation : ReasonOfChanges.StartSales;

                    if (lastChange.MinPrice == 0)
                        return freshData.MinPrice == 0 ? ReasonOfChanges.NothingChanged : ReasonOfChanges.StartSales;

                    if (lastChange.MinPrice > freshData.MinPrice)
                        return ReasonOfChanges.PriceDecreased;

                    if (lastChange.MinPrice < freshData.MinPrice)
                        return ReasonOfChanges.PriceIncreased;

                    break;
            }

            return ReasonOfChanges.NothingChanged;
        }

        public void Dispose()
        {
        }
    }
}
