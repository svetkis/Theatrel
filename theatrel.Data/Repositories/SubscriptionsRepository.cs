using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Filters;

namespace theatrel.DataAccess.Repositories;

internal class SubscriptionsRepository : ISubscriptionsRepository
{
    private AppDbContext _dbContext;
    public SubscriptionsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SubscriptionEntity> Get(int id)
    {
        try
        {
            return GetById(id);
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList subscription dbException {ex.Message}");
        }

        return Task.FromResult<SubscriptionEntity>(null);
    }

    public IEnumerable<SubscriptionEntity> GetAllWithFilter()
    {
        try
        {
            return _dbContext.Subscriptions
                .Include(s => s.PerformanceFilter)
                .AsNoTracking()
                .ToArray();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList subscription with filter DbException {ex.Message}");
        }

        return Array.Empty<SubscriptionEntity>();
    }

    public IEnumerable<VkSubscriptionEntity> GetAllWithFilterVk()
    {
        try
        {
            return _dbContext.VkSubscription
                .Include(s => s.PerformanceFilter)
                .AsNoTracking()
                .ToArray();
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"GetList vk subscription with filter DbException {ex.Message}");
        }

        return Array.Empty<VkSubscriptionEntity>();
    }

    private Task<SubscriptionEntity> GetById(int id)
        => _dbContext.Subscriptions
        .AsNoTracking()
        .SingleOrDefaultAsync(u => u.Id == id);

    public SubscriptionEntity[] GetUserSubscriptions(long userId)
    {
        try
        {
            return _dbContext.Subscriptions
                .Include(s => s.PerformanceFilter)
                .Where(s => s.TelegramUserId == userId).AsNoTracking().ToArray();
        }
        catch (Exception e)
        {
            Trace.TraceError($"GetUserSubscriptions db exceptions {e.Message} {e.InnerException?.Message}");
            return Array.Empty<SubscriptionEntity>();
        }
    }

    public VkSubscriptionEntity[] GetUserSubscriptionsVk(long userId)
    {
        try
        {
            return _dbContext.VkSubscription
                .Include(s => s.PerformanceFilter)
                .Where(s => s.VkId == userId).AsNoTracking().ToArray();
        }
        catch (Exception e)
        {
            Trace.TraceError($"GetUserSubscriptions db exceptions {e.Message} {e.InnerException?.Message}");
            return Array.Empty<VkSubscriptionEntity>();
        }
    }

    public IEnumerable<SubscriptionEntity> GetOutdatedList()
    {
        SubscriptionEntity[] outdatedByDate = _dbContext.Subscriptions
            .Where(s =>
                string.IsNullOrEmpty(s.PerformanceFilter.Actor) &&
                s.PerformanceFilter.PlaybillId == -1 &&
                null == s.PerformanceFilter.PerformanceName &&
                s.SubscriptionType < 1 &&
                s.PerformanceFilter.EndDate < DateTime.UtcNow)
            .Include(s => s.PerformanceFilter)
            .AsNoTracking()
            .ToArray();

        SubscriptionEntity[] byPlaybillId = _dbContext.Subscriptions.Where(s =>
                s.PerformanceFilter.PlaybillId != -1).Include(s => s.PerformanceFilter).AsNoTracking()
            .ToArray();

        if (!byPlaybillId.Any())
            return outdatedByDate;

        List<SubscriptionEntity> outdatedList = new (outdatedByDate);
        foreach (var subscription in byPlaybillId)
        {
            var pb = _dbContext.Playbill
                .Where(p => p.Id == subscription.PerformanceFilter.PlaybillId)
                .AsNoTracking()
                .FirstOrDefault();

            if (pb == null || pb.When < DateTime.UtcNow)
                outdatedList.Add(subscription);
        }

        return outdatedList.ToArray();
    }

    public IEnumerable<VkSubscriptionEntity> GetOutdatedListVk()
    {
        VkSubscriptionEntity[] outdatedByDate = _dbContext.VkSubscription
            .Where(s =>
                string.IsNullOrEmpty(s.PerformanceFilter.Actor) &&
                s.PerformanceFilter.PlaybillId == -1 &&
                null == s.PerformanceFilter.PerformanceName &&
                s.SubscriptionType < 1 &&
                s.PerformanceFilter.EndDate < DateTime.UtcNow)
            .Include(s => s.PerformanceFilter)
            .AsNoTracking()
            .ToArray();

        VkSubscriptionEntity[] byPlaybillId = _dbContext.VkSubscription.Where(s =>
                s.PerformanceFilter.PlaybillId != -1).Include(s => s.PerformanceFilter).AsNoTracking()
            .ToArray();

        if (!byPlaybillId.Any())
            return outdatedByDate;

        List<VkSubscriptionEntity> outdatedList = new(outdatedByDate);
        foreach (var subscription in byPlaybillId)
        {
            var pb = _dbContext.Playbill
                .Where(p => p.Id == subscription.PerformanceFilter.PlaybillId)
                .AsNoTracking()
                .FirstOrDefault();

            if (pb == null || pb.When < DateTime.UtcNow)
                outdatedList.Add(subscription);
        }

        return outdatedList.ToArray();
    }

    public async Task<SubscriptionEntity> Create(long userId, int reasonOfChange, IPerformanceFilter filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var filterEntity = new PerformanceFilterEntity
            {
                PerformanceName = filter.PerformanceName,
                Actor = filter.Actor,
                PlaybillId = filter.PlaybillId,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                DaysOfWeek = filter.DaysOfWeek,
                TheatreIds = filter.TheatreIds,
                LocationIds = filter.LocationIds,
                PerformanceTypes = filter.PerformanceTypes,
            };

            //check if playbillEntry exists
            if (filter.PlaybillId != -1)
            {
                var pbEntity = _dbContext.Playbill
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Id == filter.PlaybillId);

                if (pbEntity == null)
                    return null;
            }

            TelegramUserEntity userEntity = await _dbContext.TlUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);
            bool newUserEntity = null == userEntity;

            if (newUserEntity)
            {
                Trace.TraceInformation($"User {userId} not found, we will create item");
                userEntity = new TelegramUserEntity { Culture = "ru", Id = userId };
            }

            SubscriptionEntity entity = new SubscriptionEntity
            {
                TelegramUser = userEntity,
                PerformanceFilter = filterEntity,
                LastUpdate = DateTime.UtcNow,
                TrackingChanges = reasonOfChange
            };

            _dbContext.Subscriptions.Add(entity);
            _dbContext.Add(filterEntity);
            if (newUserEntity)
                _dbContext.Add(userEntity);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.Entry(userEntity).State = EntityState.Detached;

            return entity;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Create subscription DbException {ex.Message} inner exception {ex.InnerException?.Message}");
            return null;
        }
    }

    public async Task<VkSubscriptionEntity> CreateVk(long userId, int reasonOfChange, IPerformanceFilter filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var filterEntity = new PerformanceFilterEntity
            {
                PerformanceName = filter.PerformanceName,
                Actor = filter.Actor,
                PlaybillId = filter.PlaybillId,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                DaysOfWeek = filter.DaysOfWeek,
                TheatreIds = filter.TheatreIds,
                LocationIds = filter.LocationIds,
                PerformanceTypes = filter.PerformanceTypes,
            };

            //check if playbillEntry exists
            if (filter.PlaybillId != -1)
            {
                var pbEntity = _dbContext.Playbill
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Id == filter.PlaybillId);

                if (pbEntity == null)
                    return null;
            }


            var entity = new VkSubscriptionEntity
            {
                VkId = userId,
                PerformanceFilter = filterEntity,
                LastUpdate = DateTime.UtcNow,
                TrackingChanges = reasonOfChange
            };

            _dbContext.VkSubscription.Add(entity);
            _dbContext.Add(filterEntity);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return entity;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Create vk subscription DbException {ex.Message} inner exception {ex.InnerException?.Message}");
            return null;
        }
    }

    public async Task<bool> Delete(SubscriptionEntity entity)
    {
        _dbContext.Entry(entity.PerformanceFilter).State = EntityState.Deleted;
        _dbContext.Entry(entity).State = EntityState.Deleted;

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to delete subscription {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteVk(VkSubscriptionEntity entity)
    {
        _dbContext.Entry(entity.PerformanceFilter).State = EntityState.Deleted;
        _dbContext.Entry(entity).State = EntityState.Deleted;

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to delete subscription {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteFilter(PerformanceFilterEntity entity)
    {
        _dbContext.Entry(entity).State = EntityState.Deleted;

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to delete subscription {ex.Message}");
            return false;
        }
    }


    public async Task<bool> DeleteRange(IEnumerable<SubscriptionEntity> entities)
    {
        foreach (var entity in entities)
        {
            _dbContext.Entry(entity.PerformanceFilter).State = EntityState.Deleted;
            _dbContext.Entry(entity).State = EntityState.Deleted;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to delete subscription {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateDate(int id)
    {
        SubscriptionEntity subscription = await GetById(id);

        if (subscription == null)
            return false;

        subscription.LastUpdate = DateTime.Now;

        _dbContext.Entry(subscription).State = EntityState.Modified;

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to update subscription {ex.Message}");
            return false;
        }
    }

    public PlaybillChangeEntity[] GetFreshChanges(DateTime lastUpdate) =>
        _dbContext.PlaybillChanges
            .Where(c => c.LastUpdate > lastUpdate)
            .Include(c => c.PlaybillEntity)
            .ThenInclude(p => p.Performance)
            .ThenInclude(p => p.Type)
            .Include(c => c.PlaybillEntity)
            .ThenInclude(p => p.Performance)
            .ThenInclude(p => p.Location)
            .ThenInclude(p => p.Theatre)
            .Include(c => c.PlaybillEntity)
            .ThenInclude(p => p.Cast)
            .ThenInclude(p => p.Actor)
            .Include(c => c.PlaybillEntity)
            .ThenInclude(p => p.Cast)
            .ThenInclude(p => p.Role)
            .AsNoTracking().ToArray();

    public void Dispose()
    {
        if (_dbContext == null)
            return;

        _dbContext.Dispose();
        _dbContext = null;
    }
}