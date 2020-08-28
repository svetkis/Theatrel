using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using theatrel.Common.Enums;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Playbill;

namespace theatrel.DataAccess.Repositories
{
    internal class PlaybillRepository : IPlaybillRepository
    {
        private readonly AppDbContext _dbContext;

        public PlaybillRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private PerformanceEntity GetPerformanceEntity(IPerformanceData data) => GetPerformanceEntity(data, out _, out _);

        private PerformanceEntity GetPerformanceEntity(IPerformanceData data, out LocationsEntity location, out PerformanceTypeEntity type)
        {
            LocationsEntity l = _dbContext.PerformanceLocations.AsNoTracking()
                .FirstOrDefault(x => x.Name == data.Location);
            location = l;

            PerformanceTypeEntity t = _dbContext.PerformanceTypes.AsNoTracking().FirstOrDefault(x => x.TypeName == data.Type);
            type = t;

            if (l == null || t == null)
                return null;

            return _dbContext.Performances.AsNoTracking()
                .FirstOrDefault(p => p.Location == l && p.Type == t && p.Name == data.Name);
        }

        private PerformanceEntity AddPerformanceEntity(IPerformanceData data, LocationsEntity location, PerformanceTypeEntity type)
        {
            var performance = new PerformanceEntity
            {
                Name = data.Name,
                Location = location ?? new LocationsEntity { Name = data.Location },
                Type = type ?? new PerformanceTypeEntity { TypeName = data.Type },
            };

            _dbContext.Performances.Add(performance);

            return performance;
        }

        public async Task<PlaybillEntity> AddPlaybill(IPerformanceData data)
        {
            try
            {
                var performance = GetPerformanceEntity(data, out var location, out var type)
                                  ?? AddPerformanceEntity(data, location, type);

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

                _dbContext.Playbill.Add(playBillEntry);
                _dbContext.Add(change);

                await _dbContext.SaveChangesAsync();

                return playBillEntry;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
            }

            return null;
        }

        public PlaybillEntity Get(IPerformanceData data)
        {
            try
            {
                var performance = GetPerformanceEntity(data);
                if (performance == null)
                    return null;

                return _dbContext.Playbill.AsNoTracking()
                    .Include(x => x.Changes)
                    .FirstOrDefault(x => x.When == data.DateTime && x.PerformanceId == performance.Id);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
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

            entity.Changes.Add(change);
            _dbContext.Add(change);

            _dbContext.Entry(entity).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
                return false;
            }
        }

        private Task<PlaybillChangeEntity> GetChangeById(long id)
            => _dbContext.PerformanceChanges.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);

        public async Task<bool> Update(PlaybillChangeEntity entity)
        {
            PlaybillChangeEntity oldValue = await GetChangeById(entity.Id);

            if (oldValue == null)
                return false;

            _dbContext.Entry(entity).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"DbException {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
