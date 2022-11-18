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

namespace theatrel.DataUpdater;

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

    public async Task<bool> Update(int theaterId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        Trace.TraceInformation($"PlaybillUpdater started. {theaterId}");

        using var dbRepository = _dbService.GetPlaybillRepository();

        var dateFilter = _filterService.GetOneMonthFilter(startDate);

        IPerformanceData[] performances = await _dataResolver.RequestProcess(theaterId, dateFilter, cancellationToken);

        await _dataResolver.AdditionalProcess(theaterId, performances, cancellationToken);

        dbRepository.EnsureCreateTheatre(theaterId, theaterId == 1 ? "Мариинский театр" : "Михайловский театр");
        foreach (var freshPerformanceData in performances)
        {
            await ProcessDataInternal(freshPerformanceData, dbRepository);
        }        

        Trace.TraceInformation("PlaybillUpdater finished.");
        return true;
    }

    private async Task ProcessDataInternal(IPerformanceData data, IPlaybillRepository playbillRepository)
    {
        if (data.DateTime < DateTime.UtcNow)
            return;

        PlaybillEntity playbillEntry = playbillRepository.GetPlaybillByPerformanceData(data);
        if (null == playbillEntry)
        {
            if (data.TicketsUrl == CommonTags.WasMovedTag)
                return;

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

        if (!string.Equals(playbillEntry.Description, data.Description))
        {
            await playbillRepository.UpdateDescription(playbillEntry.Id, data.Description);
        }

        ReasonOfChanges compareCastResult = CompareCast(playbillRepository, playbillEntry, data, out string[] added, out string[] removed);

        if (_castChangedReasons.Contains(compareCastResult))
        {
            bool result = await playbillRepository.UpdateCast(playbillEntry, data);
            if (result && compareCastResult != ReasonOfChanges.CastOnlyUrlChanged)
            {
                await playbillRepository.AddChange(playbillEntry.Id, new PlaybillChangeEntity
                {
                    CastAdded = string.Join(',', added),
                    CastRemoved = string.Join(',', removed),
                    LastUpdate = DateTime.UtcNow,
                    MinPrice = data.MinPrice,
                    ReasonOfChanges = (int)compareCastResult
                });
            }
        }

        var compareResult = ComparePerformanceData(lastChange, data);
        switch (compareResult)
        {
            case ReasonOfChanges.None:
                return;
            case ReasonOfChanges.DataError:
                Trace.TraceInformation($"Data error {data.Name} {data.DateTime:M} price: {data.MinPrice}");
                return;
        }

        await playbillRepository.AddChange(playbillEntry.Id, new PlaybillChangeEntity
        {
            LastUpdate = DateTime.UtcNow,
            MinPrice = data.MinPrice,
            ReasonOfChanges = (int)compareResult,
        });
    }

    private static ReasonOfChanges ComparePerformanceData(PlaybillChangeEntity lastChange, IPerformanceData freshData)
    {
        switch (freshData.State)
        {
            case TicketsState.TechnicalError:
                return ReasonOfChanges.DataError;

            case TicketsState.PerformanceWasMoved:
                return lastChange != null && lastChange.ReasonOfChanges == (int)ReasonOfChanges.WasMoved
                    ? ReasonOfChanges.None
                    : ReasonOfChanges.WasMoved;

            case TicketsState.NoTickets:
                if (lastChange == null)
                    return ReasonOfChanges.Creation;

                return lastChange.MinPrice == 0
                    ? ReasonOfChanges.None
                    : ReasonOfChanges.StopSales;

            case TicketsState.Ok:
                if (lastChange == null)
                    return freshData.MinPrice == 0 ? ReasonOfChanges.Creation : ReasonOfChanges.StartSales;

                if (lastChange.MinPrice == 0)
                    return freshData.MinPrice == 0 ? ReasonOfChanges.None : ReasonOfChanges.StartSales;

                if (freshData.MinPrice == 0)
                    return ReasonOfChanges.StopSale;

                if (lastChange.MinPrice > freshData.MinPrice)
                    return ReasonOfChanges.PriceDecreased;

                if (lastChange.MinPrice < freshData.MinPrice)
                    return ReasonOfChanges.PriceIncreased;

                break;
        }

        return ReasonOfChanges.None;
    }

    private ReasonOfChanges[] _castChangedReasons = new ReasonOfChanges[]
    {
        ReasonOfChanges.CastWasSet,
        ReasonOfChanges.CastWasChanged,
        ReasonOfChanges.CastOnlyUrlChanged
    };

    private ReasonOfChanges CompareCast(IPlaybillRepository playbillRepository, PlaybillEntity playbillEntity, IPerformanceData freshData, out string[] added, out string[] removed)
    {
        added = Array.Empty<string>();
        removed = Array.Empty<string>();

        switch (freshData.Cast.State)
        {
            case CastState.TechnicalError:
            case CastState.PerformanceWasMoved:
                return ReasonOfChanges.None;

            case CastState.CastIsNotSet:
                return playbillEntity.Cast.Any() ? ReasonOfChanges.CastWasChanged : ReasonOfChanges.None;

            case CastState.Ok:
                return playbillRepository.CompareCast(playbillEntity, freshData, out added, out removed);
        }

        return ReasonOfChanges.None;
    }
}