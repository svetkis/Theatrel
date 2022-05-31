using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Playbill;

namespace theatrel.DataAccess.Repositories;

internal class PlaybillRepository : IPlaybillRepository
{
    private AppDbContext _dbContext;

    public PlaybillRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private int GetPerformanceEntityId(IPerformanceData data) => GetPerformanceEntityId(data, out _, out _);

    private int GetPerformanceEntityId(IPerformanceData data, out int locationId, out int typeId)
    {
        LocationsEntity l = _dbContext.PerformanceLocations.AsNoTracking()
            .FirstOrDefault(x => x.Name == data.Location);
        locationId = l?.Id ?? -1;

        PerformanceTypeEntity t = _dbContext.PerformanceTypes.AsNoTracking().FirstOrDefault(x => x.TypeName == data.Type);
        typeId = t?.Id ?? -1;

        if (l == null || t == null)
            return -1;

        var performanceId = _dbContext.Performances.AsNoTracking()
            .FirstOrDefault(p => p.LocationId == l.Id && p.TypeId == t.Id && p.Name == data.Name)?.Id ?? -1;

        return performanceId;
    }

    private int GetRoleId(string characterName, int performanceId)
    {
        RoleEntity role = _dbContext.Roles.AsNoTracking().FirstOrDefault(x => x.CharacterName == characterName && x.PerformanceId == performanceId);
        return role?.Id ?? -1;
    }


    private int GetActorId(string actorName, string actorUrl)
    {
        var actor = actorUrl != CommonTags.NotDefinedTag
            ? _dbContext.Actors.AsNoTracking().FirstOrDefault(x => x.Url == actorUrl)
            : _dbContext.Actors.AsNoTracking().FirstOrDefault(x => x.Name == actorName);

        return actor?.Id ?? -1;
    }

    private IList<ActorInRoleEntity> AddActorsInRole(string roleName, IList<IActor> actorsData, PerformanceEntity performance, int playbillId, int performanceId)
    {
        try
        {
            int roleId = -1 == performanceId ? -1 : GetRoleId(roleName, performance.Id);

            RoleEntity role = roleId != -1
                ? _dbContext.Roles.FirstOrDefault(l => l.Id == roleId)
                : new RoleEntity { CharacterName = roleName, Performance = performance };

            List<ActorInRoleEntity> actorsInRoleList = new List<ActorInRoleEntity>();

            foreach (var actorData in actorsData)
            {
                int actorId = GetActorId(actorData.Name, actorData.Url);

                ActorEntity actor = actorId != -1
                    ? _dbContext.Actors.FirstOrDefault(t => t.Id == actorId)
                    : new ActorEntity { Name = actorData.Name, Url = actorData.Url };

                ActorInRoleEntity actorInRole = actorId != -1 && roleId != -1 && playbillId != -1
                    ? _dbContext.ActorInRole.FirstOrDefault(ar => ar.ActorId == actorId && ar.RoleId == roleId && ar.PlaybillId == playbillId)
                      ?? new ActorInRoleEntity {Role = role, Actor = actor}
                    : new ActorInRoleEntity { Role = role, Actor = actor };

                actorsInRoleList.Add(actorInRole);
            }

            return actorsInRoleList;
        }
        catch (Exception e)
        {
            Console.WriteLine($"AddActorsInRole DbException: {e.Message} {e.InnerException?.Message}");
            throw;
        }
    }

    private PerformanceEntity AddPerformanceEntity(IPerformanceData data, int locationId, int typeId)
    {
        try
        {
            LocationsEntity location = locationId != -1
                ? _dbContext.PerformanceLocations.FirstOrDefault(l => l.Id == locationId)
                : new LocationsEntity { Name = data.Location, Theatre = _theatre };

            PerformanceTypeEntity type = typeId != -1
                ? _dbContext.PerformanceTypes.FirstOrDefault(t => t.Id == typeId)
                : new PerformanceTypeEntity { TypeName = data.Type };

            PerformanceEntity performance = new PerformanceEntity
            {
                Name = data.Name,
                Location = location,
                Type = type
            };

            _dbContext.Performances.Add(performance);

            return performance;
        }
        catch (Exception e)
        {
            Console.WriteLine($"AddPerformanceEntity DbException: {e.Message} {e.InnerException?.Message}");
            throw;
        }
    }

    private TheatreEntity _theatre;
    public void SetTheatre(int theatreId, string theatreName)
    {
        try
        {
            _theatre = _dbContext.Theatre.FirstOrDefault(t => t.Id == theatreId);
            if (_theatre == null)
            {
                _theatre = new TheatreEntity { Id = theatreId, Name = theatreName };
                _dbContext.Theatre.Add(_theatre);
                _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"GetOrCreateTheatreEntity DbException: {e.Message} {e.InnerException?.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateCast(PlaybillEntity playbillEntry, IPerformanceData data)
    {
        var checkList = playbillEntry.Cast.Select(a => new CheckData { Exists = false, ActorInRole = a }).ToArray();
        var toAddListData = new Dictionary<string, IList<IActor>>();

        foreach (KeyValuePair<string, IList<IActor>> castDataFresh in data.Cast.Cast)
        {
            string character = castDataFresh.Key;
            foreach (var actorFresh in castDataFresh.Value)
            {
                var checkItem = actorFresh.Url == CommonTags.NotDefinedTag
                    ? checkList.FirstOrDefault(item =>
                        item.ActorInRole.Role.CharacterName == character &&
                        item.ActorInRole.Actor.Name == actorFresh.Name)
                    : checkList.FirstOrDefault(item =>
                        item.ActorInRole.Role.CharacterName == character &&
                        item.ActorInRole.Actor.Url == actorFresh.Url);

                if (checkItem == null)
                {
                    if (!toAddListData.ContainsKey(character))
                        toAddListData[character] = new List<IActor>();

                    toAddListData[character].Add(actorFresh);
                    continue;
                }

                checkItem.Exists = true;
            }
        }

        ActorInRoleEntity[] toRemoveList = checkList.Where(item => !item.Exists).Select(c => c.ActorInRole).ToArray();
        ActorInRoleEntity[] toAddList = toAddListData.Select(data
                => AddActorsInRole(data.Key, data.Value, playbillEntry.Performance, playbillEntry.Id,
                    playbillEntry.PerformanceId))
            .SelectMany(group => group.ToArray()).ToArray();

        PlaybillEntity oldValue = await GetById(playbillEntry.Id);

        if (oldValue == null)
            return false;

        try
        {
            foreach (ActorInRoleEntity removedItem in toRemoveList)
            {
                playbillEntry.Cast.Remove(removedItem);
                _dbContext.Entry(removedItem).State = EntityState.Deleted;
            }

            foreach (var addItem in toAddList)
                playbillEntry.Cast.Add(addItem);

            _dbContext.AddRange(toAddList);

            _dbContext.Entry(playbillEntry).State = EntityState.Modified;
            _dbContext.Entry(playbillEntry.Performance).State = EntityState.Modified;

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation(
                $"UpdateCast DbException {ex.Message} InnerException {ex.InnerException?.Message}");

            return false;
        }
        finally
        {
            _dbContext.Entry(playbillEntry).State = EntityState.Detached;
            _dbContext.Entry(playbillEntry.Performance).State = EntityState.Detached;
            if (playbillEntry.Cast != null && playbillEntry.Cast.Any())
            {
                foreach (var castItem in playbillEntry.Cast)
                {
                    _dbContext.Entry(castItem).State = EntityState.Detached;
                    _dbContext.Entry(castItem.Actor).State = EntityState.Detached;
                    _dbContext.Entry(castItem.Role).State = EntityState.Detached;
                }
            }
        }

        return true;
    }

    private sealed class CheckData
    {
        public bool Exists { get; set; }
        public ActorInRoleEntity ActorInRole { get; set; }
    }

    public bool IsCastEqual(PlaybillEntity playbillEntry, IPerformanceData data)
    {
        CheckData[] checkList = playbillEntry.Cast.Select(a => new CheckData { Exists = false, ActorInRole = a}).ToArray();
        foreach (KeyValuePair<string, IList<IActor>> castDataFresh in data.Cast.Cast)
        {
            string character = castDataFresh.Key;
            foreach (var actorFresh in castDataFresh.Value)
            {
                var checkItem = actorFresh.Url == CommonTags.NotDefinedTag
                    ? checkList.FirstOrDefault(item =>
                        item.ActorInRole.Role.CharacterName == character &&
                        item.ActorInRole.Actor.Name == actorFresh.Name)
                    : checkList.FirstOrDefault(item =>
                        item.ActorInRole.Role.CharacterName == character &&
                        item.ActorInRole.Actor.Url == actorFresh.Url);

                if (checkItem == null)
                {
                    Trace.TraceInformation($"Cast is not equal {playbillEntry.Id}. Not found {character} - {actorFresh.Name} {actorFresh.Url}");
                    return false;
                }

                checkItem.Exists = true;
            }
        }

        bool result = checkList.All(item => item.Exists);
        if (!result)
        {
            var changedData = checkList.Where(item => !item.Exists).ToArray();
            if (changedData.Any())
            {
                Trace.TraceInformation($"Cast is not equal {playbillEntry.Id}.");
                foreach (var item in changedData)
                {
                    Trace.TraceInformation($"No data in fresh {item.ActorInRole.Role.CharacterName} - {item.ActorInRole.Actor.Name}");
                }
            }
        }

        return checkList.All(item => item.Exists);
    }

    public async Task<PlaybillEntity> AddPlaybill(IPerformanceData data, int reasonOfFirstChanges)
    {
        PerformanceEntity performance = null;
        PlaybillEntity playBillEntry = null;

        try
        {
            int performanceId = GetPerformanceEntityId(data, out int locationId, out int typeId);
            performance = -1 == performanceId
                ? AddPerformanceEntity(data, locationId, typeId)
                : _dbContext.Performances
                    .Include(p => p.Location)
                    .Include(p => p.Type)
                    .FirstOrDefault(p => p.Id == performanceId);

            List<ActorInRoleEntity> castList = new List<ActorInRoleEntity>();
            if (data.Cast.State == CastState.Ok)
            {
                foreach (KeyValuePair<string, IList<IActor>> castData in data.Cast.Cast)
                {
                    IList<IActor> actorsData = castData.Value;
                    string characterName = castData.Key;

                    castList.AddRange(AddActorsInRole(characterName, actorsData, performance, -1, performanceId));
                }
            }

            playBillEntry = new PlaybillEntity
            {
                Performance = performance,
                TicketsUrl = data.TicketsUrl,
                Url = data.Url,
                When = data.DateTime,
                Changes = new List<PlaybillChangeEntity>
                {
                    new PlaybillChangeEntity
                    {
                        LastUpdate = DateTime.UtcNow,
                        MinPrice = data.MinPrice,
                        ReasonOfChanges = reasonOfFirstChanges,
                    }
                },
                Cast = castList,
                Description = data.Description
            };

            _dbContext.Playbill.Add(playBillEntry);
            _dbContext.Add(playBillEntry.Changes.First());

            await _dbContext.SaveChangesAsync();

            return playBillEntry;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"AddPlaybill DbException {ex.Message} InnerException {ex.InnerException?.Message}");
        }
        finally
        {
            if (performance != null)
            {
                _dbContext.Entry(performance.Location).State = EntityState.Detached;
                _dbContext.Entry(performance.Type).State = EntityState.Detached;
                _dbContext.Entry(performance).State = EntityState.Detached;
            }

            if (playBillEntry != null)
            {
                _dbContext.Entry(playBillEntry).State = EntityState.Detached;
                _dbContext.Entry(playBillEntry.Changes.First()).State = EntityState.Detached;

                if (playBillEntry.Cast != null)
                {
                    foreach (var castItem in playBillEntry.Cast)
                    {
                        _dbContext.Entry(castItem).State = EntityState.Detached;
                        _dbContext.Entry(castItem.Actor).State = EntityState.Detached;
                        _dbContext.Entry(castItem.Role).State = EntityState.Detached;
                    }
                }
            }
        }

        return null;
    }

    public IEnumerable<PlaybillEntity> GetOutdatedList()
    {
        try
        {
            return _dbContext.Playbill.Where(x => x.When < DateTime.UtcNow).AsNoTracking().ToArray();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList PlaybillEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
        }

        return Array.Empty<PlaybillEntity>();
    }

    public IEnumerable<PerformanceEntity> GetOutdatedPerformanceEntities()
    {
        try
        {
            List<PerformanceEntity> result = new List<PerformanceEntity>();
            foreach (var performanceEntity in _dbContext.Performances.AsNoTracking().ToArray())
            {
                if (!_dbContext.Playbill.Where(x => x.PerformanceId == performanceEntity.Id).AsNoTracking().Any())
                    result.Add(performanceEntity);
            }

            return result;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList PlaybillEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
        }

        return Array.Empty<PerformanceEntity>();
    }

    public IEnumerable<PlaybillEntity> GetList(DateTime from, DateTime to)
    {
        try
        {
            return _dbContext.Playbill
                .Where(x => x.When <= to && x.When >= from)
                .Include(x => x.Performance)
                .ThenInclude(x => x.Location)
                .Include(x => x.Performance)
                .ThenInclude(x => x.Type)
                .Include(x => x.Changes)
                .AsNoTracking().ToArray();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList PlaybillEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
        }

        return Array.Empty<PlaybillEntity>();
    }

    public IEnumerable<PlaybillEntity> GetListByName(string name)
    {
        try
        {
            string lowerName = name.ToLower();
            return _dbContext.Playbill
                .Where(x => x.Performance.Name.ToLower().Contains(lowerName))
                .Include(x => x.Performance)
                .ThenInclude(x => x.Location)
                .Include(x => x.Performance)
                .ThenInclude(x => x.Type)
                .Include(x => x.Changes)
                .AsNoTracking().ToArray();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList PlaybillEntity by name DbException {ex.Message} InnerException {ex.InnerException?.Message}");
        }

        return Array.Empty<PlaybillEntity>();
    }

    public PlaybillEntity Get(IPerformanceData data)
    {
        try
        {
            var performanceId = GetPerformanceEntityId(data);
            if (-1 == performanceId)
                return null;

            return _dbContext.Playbill
                .Where(x => x.When == data.DateTime && x.PerformanceId == performanceId)
                .Include(x => x.Performance)
                .Include(x => x.Changes)
                .Include(x => x.Cast)
                .ThenInclude(c => c.Actor)
                .Include(x => x.Cast)
                .ThenInclude(c => c.Role)
                .AsNoTracking()
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList PlaybillEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
        }

        return null;
    }

    public PlaybillEntity Get(int id)
    {
        try
        {
            return _dbContext.Playbill.Where(x => x.Id == id).AsNoTracking().FirstOrDefault();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList PlaybillEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
        }

        return null;
    }

    public PlaybillEntity GetWithName(int id)
    {
        try
        {
            return _dbContext.Playbill.Where(x => x.Id == id)
                .Include(p => p.Performance)
                .AsNoTracking().FirstOrDefault();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList PlaybillEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
        }

        return null;
    }

    private Task<PlaybillEntity> GetById(long id)
        => _dbContext.Playbill.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);

    public async Task<bool> AddChange(int playbillEntityId, PlaybillChangeEntity change)
    {
        PlaybillEntity oldValue = await GetById(playbillEntityId);

        if (oldValue == null)
            return false;

        PlaybillEntity playbillEntity = GetTrackedWithAllIncludesById(playbillEntityId);
        try
        {
            playbillEntity.Changes.Add(change);
            _dbContext.Add(change);

            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"AddChange DbException {ex.Message} InnerException {ex.InnerException?.Message}");
            return false;
        }
        finally
        {
            if (playbillEntity != null)
            {
                _dbContext.Entry(playbillEntity.Performance.Location).State = EntityState.Detached;
                _dbContext.Entry(playbillEntity.Performance.Type).State = EntityState.Detached;
                _dbContext.Entry(playbillEntity.Performance).State = EntityState.Detached;
                foreach (var ch in playbillEntity.Changes)
                    _dbContext.Entry(ch).State = EntityState.Detached;

                _dbContext.Entry(playbillEntity).State = EntityState.Detached;
            }
        }
    }

    private Task<PlaybillChangeEntity> GetChangeById(long id)
        => _dbContext.PlaybillChanges.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);

    public async Task<bool> UpdateChangeLastUpdate(int changeId)
    {
        PlaybillChangeEntity oldValue = await GetChangeById(changeId);

        if (oldValue == null)
            return false;

        try
        {
            oldValue.LastUpdate = DateTime.UtcNow;
            _dbContext.Entry(oldValue).State = EntityState.Modified;

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Update Change entity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
            return false;
        }
        finally
        {
            _dbContext.Entry(oldValue).State = EntityState.Detached;
        }
    }

    private PlaybillEntity GetTrackedWithAllIncludesById(int playbillEntryId) => _dbContext.Playbill
        .Include(p => p.Performance)
        .ThenInclude(p => p.Type)
        .Include(p => p.Performance)
        .ThenInclude(p => p.Location)
        .Include(p => p.Changes)
        .FirstOrDefault(p => p.Id == playbillEntryId);

    public async Task<bool> UpdateDescription(int playbillEntityId, string description)
    {
        PlaybillEntity oldValue = Get(playbillEntityId);

        if (oldValue == null)
        {
            Trace.TraceInformation($"UpdateDescription can't get old value for {playbillEntityId}");
            return false;
        }

        PlaybillEntity playbillEntity = null;
        try
        {
            playbillEntity = GetTrackedWithAllIncludesById(playbillEntityId);

            playbillEntity.Description = description;

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation(
                $"UpdateTicketsUrl DbException {ex.Message} InnerException {ex.InnerException?.Message}");
            return false;
        }
        finally
        {
            if (playbillEntity != null)
            {
                _dbContext.Entry(playbillEntity.Performance.Location).State = EntityState.Detached;
                _dbContext.Entry(playbillEntity.Performance.Type).State = EntityState.Detached;
                _dbContext.Entry(playbillEntity.Performance).State = EntityState.Detached;
                foreach (var change in playbillEntity.Changes)
                    _dbContext.Entry(change).State = EntityState.Detached;

                _dbContext.Entry(playbillEntity).State = EntityState.Detached;
            }
        }
    }

    public async Task<bool> UpdateTicketsUrl(int playbillEntityId, string url)
    {
        PlaybillEntity oldValue = Get(playbillEntityId);

        if (oldValue == null)
        {
            Trace.TraceInformation($"UpdateTicketsUrl can't get old value for {playbillEntityId}");
            return false;
        }

        PlaybillEntity playbillEntity = null;
        try
        {
            playbillEntity = GetTrackedWithAllIncludesById(playbillEntityId);

            playbillEntity.TicketsUrl = url;

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation(
                $"UpdateTicketsUrl DbException {ex.Message} InnerException {ex.InnerException?.Message}");
            return false;
        }
        finally
        {
            if (playbillEntity != null)
            {
                _dbContext.Entry(playbillEntity.Performance.Location).State = EntityState.Detached;
                _dbContext.Entry(playbillEntity.Performance.Type).State = EntityState.Detached;
                _dbContext.Entry(playbillEntity.Performance).State = EntityState.Detached;
                foreach (var change in playbillEntity.Changes)
                    _dbContext.Entry(change).State = EntityState.Detached;

                _dbContext.Entry(playbillEntity).State = EntityState.Detached;
            }
        }
    }

    public async Task<bool> UpdateUrl(int playbillEntityId, string url)
    {
        PlaybillEntity oldValue = Get(playbillEntityId);

        if (oldValue == null)
            return false;

        PlaybillEntity playbillEntity = null;
        try
        {
            playbillEntity = GetTrackedWithAllIncludesById(playbillEntityId);

            playbillEntity.Url = url;

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"UpdateUrl DbException {ex.Message} InnerException {ex.InnerException?.Message}");
            return false;
        }
        finally
        {
            if (playbillEntity != null)
            {
                _dbContext.Entry(playbillEntity.Performance.Location).State = EntityState.Detached;
                _dbContext.Entry(playbillEntity.Performance.Type).State = EntityState.Detached;
                _dbContext.Entry(playbillEntity.Performance).State = EntityState.Detached;
                foreach (var change in playbillEntity.Changes)
                    _dbContext.Entry(change).State = EntityState.Detached;

                _dbContext.Entry(playbillEntity).State = EntityState.Detached;
            }
        }
    }


    public async Task<bool> Delete(PlaybillEntity entity)
    {
        try
        {
            _dbContext.Playbill.Remove(entity);

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Delete PlaybillEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
            return false;
        }
    }

    public async Task<bool> Delete(PerformanceEntity entity)
    {
        try
        {
            _dbContext.Performances.Remove(entity);

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Delete PerformanceEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
            return false;
        }
    }

    public LocationsEntity GetLocation(int id) => _dbContext.PerformanceLocations.AsNoTracking().FirstOrDefault(x => x.Id == id);
    public IEnumerable<LocationsEntity> GetLocationsList(int theatreId)
        => _dbContext.PerformanceLocations.Where(x => x.Theatre.Id == theatreId).AsNoTracking();

    public IEnumerable<TheatreEntity> GetTheatres() => _dbContext.Theatre.AsNoTracking();

    public void Dispose()
    {
        if (_dbContext == null)
            return;

        _dbContext.Dispose();
        _dbContext = null;
    }
}