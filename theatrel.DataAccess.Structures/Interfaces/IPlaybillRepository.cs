using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Playbill;

namespace theatrel.DataAccess.Structures.Interfaces;

public interface IPlaybillRepository : IDisposable, IDIRegistrable
{
    IEnumerable<PlaybillEntity> GetList(DateTime from, DateTime to);
    IEnumerable<PlaybillEntity> GetListByName(string name);

    IEnumerable<PlaybillEntity> GetListByActor(string actor);

    PlaybillEntity GetPlaybillByPerformanceData(IPerformanceData data);
    PlaybillEntity GetPlaybill(int id);
    PlaybillEntity GetPlaybillEntryWithPerformanceData(int id);

    IEnumerable<PlaybillEntity> GetOutdatedList();

    IEnumerable<PerformanceEntity> GetOutdatedPerformances();

    Task<PlaybillEntity> AddPlaybill(IPerformanceData data);
    Task<bool> AddChange(int playbillEntityId, PlaybillChangeEntity change);

    Task<bool> UpdateUrl(int playbillEntityId, string url);
    Task<bool> UpdateDescription(int playbillEntityId, string description);
    Task<bool> UpdateTicketsUrl(int playbillEntityId, string url);

    Task<bool> UpdateCast(PlaybillEntity playbillEntry, IPerformanceData data);
    ReasonOfChanges CompareCast(PlaybillEntity playbillEntry, IPerformanceData data, out string[] added, out string[] removed);

    Task<bool> RemovePlaybillEntry(PlaybillEntity entity);

    Task<bool> RemovePerformance(PerformanceEntity entity);

    LocationsEntity GetLocation(int id);

    IEnumerable<LocationsEntity> GetLocationsList(int theatreId);

    IEnumerable<TheatreEntity> GetTheatres();

    void EnsureCreateTheatre(int theatreId, string theatreName);
    ActorEntity[] GetActorsByNameFilter(string actor);
    IEnumerable<PlaybillEntity> GetOutdatedWithAllData();
}