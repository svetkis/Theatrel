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

            IPerformanceData[] performances = await _dataResolver.RequestProcess(theaterId, _filterService.GetFilter(startDate, endDate), cancellationToken);
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

            PlaybillEntity playbillEntry = playbillRepository.Get(data);
            if (null == playbillEntry)
            {
                if (data.TicketsUrl == CommonTags.WasMovedTag)
                    return;

                int reason = data.MinPrice == 0
                        ? (int) ReasonOfChanges.Creation
                        : (int) ReasonOfChanges.StartSales;

                await playbillRepository.AddPlaybill(data, reason);
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

            if (!string.Equals(playbillEntry.Description, data.Description))
            {
                await playbillRepository.UpdateDescription(playbillEntry.Id, data.Description);
            }

            var compareCastResult = CompareCast(playbillRepository, playbillEntry, data);
            if (data.State == TicketsState.Ok && (compareCastResult == ReasonOfChanges.CastWasSet || compareCastResult == ReasonOfChanges.CastWasChanged))
            {
                if (await playbillRepository.UpdateCast(playbillEntry, data))
                {
                    await playbillRepository.AddChange(playbillEntry.Id, new PlaybillChangeEntity
                    {
                        LastUpdate = DateTime.Now,
                        MinPrice = data.MinPrice,
                        ReasonOfChanges = (int)compareCastResult
                    });
                }
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

            await playbillRepository.AddChange(playbillEntry.Id, new PlaybillChangeEntity
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
                    if (lastChange == null)
                        return ReasonOfChanges.Creation;

                    return lastChange.MinPrice == 0
                        ? ReasonOfChanges.NothingChanged
                        : ReasonOfChanges.StopSales;

                case TicketsState.Ok:
                    if (lastChange == null)
                        return freshData.MinPrice == 0 ? ReasonOfChanges.Creation : ReasonOfChanges.StartSales;

                    if (lastChange.MinPrice == 0)
                        return freshData.MinPrice == 0 ? ReasonOfChanges.NothingChanged : ReasonOfChanges.StartSales;

                    if (freshData.MinPrice == 0)
                        return ReasonOfChanges.StopSale;

                    if (lastChange.MinPrice > freshData.MinPrice)
                        return ReasonOfChanges.PriceDecreased;

                    if (lastChange.MinPrice < freshData.MinPrice)
                        return ReasonOfChanges.PriceIncreased;

                    break;
            }

            return ReasonOfChanges.NothingChanged;
        }

        private ReasonOfChanges CompareCast(IPlaybillRepository playbillRepository, PlaybillEntity playbillEntity, IPerformanceData freshData)
        {
            switch (freshData.Cast.State)
            {
                case CastState.TechnicalError:
                case CastState.PerformanceWasMoved:
                    return ReasonOfChanges.NothingChanged;

                case CastState.CastIsNotSet:
                    return playbillEntity.Cast.Any() ? ReasonOfChanges.CastWasChanged : ReasonOfChanges.NothingChanged;

                case CastState.Ok:
                    bool wasEmpty = !playbillEntity.Cast.Any();
                    bool nowEmpty = freshData.Cast.Cast == null || !freshData.Cast.Cast.Any();

                    if (wasEmpty && nowEmpty)
                        return ReasonOfChanges.NothingChanged;

                    if (wasEmpty)
                        return ReasonOfChanges.CastWasSet;

                    return playbillRepository.IsCastEqual(playbillEntity, freshData)
                            ? ReasonOfChanges.NothingChanged
                            : ReasonOfChanges.CastWasChanged;
            }

            return ReasonOfChanges.NothingChanged;
        }

        public void Dispose()
        {
        }
    }
}
