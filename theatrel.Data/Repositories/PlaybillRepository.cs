using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Playbill;

namespace theatrel.DataAccess.Repositories
{
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

        private int GetActorInRoleEntityId(string roleName, string actorName, string actorUrl, int performanceId, out int roleId, out int actorId)
        {
            RoleEntity role = _dbContext.Roles.AsNoTracking().FirstOrDefault(x => x.CharacterName == roleName && x.PerformanceId == performanceId);
            roleId = role?.Id ?? -1;

            var actor = _dbContext.Actors.AsNoTracking().FirstOrDefault(x => x.Name == actorName  && x.Url == actorUrl);
            actorId = actor?.Id ?? -1;

            if (role == null || actor == null)
                return -1;

            var actorInRoleId = _dbContext.ActorInRole.AsNoTracking()
                .FirstOrDefault(p => p.RoleId == role.Id && p.ActorId == actor.Id )?.Id ?? -1;

            return actorInRoleId;
        }

        private ActorInRoleEntity AddActorInRole(string roleName, string actorName, string actorUrl, PerformanceEntity performance, int roleId, int actorId)
        {
            try
            {
                RoleEntity role = roleId != -1
                    ? _dbContext.Roles.FirstOrDefault(l => l.Id == roleId)
                    : new RoleEntity { CharacterName = roleName, Performance = performance };

                ActorEntity actor = actorId != -1
                    ? _dbContext.Actors.FirstOrDefault(t => t.Id == actorId)
                    : new ActorEntity { Name = actorName, Url = actorUrl};

                ActorInRoleEntity actorInRole = new ActorInRoleEntity
                {
                    Actor = actor,
                    Role = role,
                };

                _dbContext.ActorInRole.Add(actorInRole);

                return actorInRole;
            }
            catch (Exception e)
            {
                Console.WriteLine($"AddActorInRole DbException: {e.Message} {e.InnerException?.Message}");
                throw;
            }
        }

        private PerformanceEntity AddPerformanceEntity(IPerformanceData data, int locationId, int typeId)
        {
            try
            {
                LocationsEntity location = locationId != -1
                    ? _dbContext.PerformanceLocations.FirstOrDefault(l => l.Id == locationId)
                    : new LocationsEntity { Name = data.Location };

                PerformanceTypeEntity type = typeId != -1
                    ? _dbContext.PerformanceTypes.FirstOrDefault(t => t.Id == typeId)
                    : new PerformanceTypeEntity { TypeName = data.Type };

                PerformanceEntity performance = new PerformanceEntity
                {
                    Name = data.Name,
                    Location = location,
                    Type = type,
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

        public async Task<PlaybillEntity> AddPlaybill(IPerformanceData data)
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

                /*List<ActorInRoleEntity> cast = new List<ActorInRoleEntity>();
                if (data.Cast.State == CastState.Ok)
                {
                    foreach (var castData in data.Cast.Cast)
                    {
                        int actorInRoleId = GetActorInRoleEntityId(castData.Key, castData.Value.Name,
                            castData.Value.Url, performanceId, out int roleId, out int actorId);
                        var actorInRole = -1 == actorInRoleId
                            ? AddActorInRole(castData.Key, castData.Value.Name, castData.Value.Url, performance, roleId,
                                actorId)
                            : _dbContext.ActorInRole
                                .Include(p => p.Actor)
                                .Include(p => p.Role)
                                .FirstOrDefault(p => p.Id == actorInRoleId);

                        cast.Add(actorInRole);
                    }
                }*/

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
                            LastUpdate = DateTime.Now,
                            MinPrice = data.MinPrice,
                            ReasonOfChanges = (int) ReasonOfChanges.Creation,
                        }
                    },
                    //                    Cast = cast
                };

                _dbContext.Playbill.Add(playBillEntry);
                _dbContext.Add(playBillEntry.Changes.First());

                await _dbContext.SaveChangesAsync();

                return playBillEntry;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(
                    $"AddPlaybill DbException {ex.Message} InnerException {ex.InnerException?.Message}");
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
                            _dbContext.Entry(castItem).State = EntityState.Detached;
                    }
                }
            }

            return null;
        }

        public IEnumerable<PlaybillEntity> GetOutdatedList()
        {
            try
            {
                return _dbContext.Playbill.Where(x => x.When < DateTime.Now).AsNoTracking().ToArray();
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"GetList PlaybillEntity DbException {ex.Message} InnerException {ex.InnerException?.Message}");
            }

            return null;
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

            return null;
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

            return null;
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

        public async Task<bool> AddChange(PlaybillEntity entity, PlaybillChangeEntity change)
        {
            PlaybillEntity oldValue = await GetById(entity.Id);

            if (oldValue == null)
                return false;

            try
            {
                entity.Changes.Add(change);
                _dbContext.Add(change);

                _dbContext.Entry(entity).State = EntityState.Modified;

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
                _dbContext.Entry(entity).State = EntityState.Detached;
                _dbContext.Entry(change).State = EntityState.Detached;
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
                oldValue.LastUpdate = DateTime.Now;
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

        public async Task<bool> UpdateTicketsUrl(int playbillEntityId, string url)
        {
            PlaybillEntity oldValue = Get(playbillEntityId);

            if (oldValue == null)
                return false;

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
                Trace.TraceInformation(
                    $"UpdateUrl DbException {ex.Message} InnerException {ex.InnerException?.Message}");
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

        public IEnumerable<LocationsEntity> GetLocationsList() => _dbContext.PerformanceLocations.AsNoTracking();

        public void Dispose()
        {
            if (_dbContext == null)
                return;

            _dbContext.Dispose();
            _dbContext = null;
        }
    }
}
