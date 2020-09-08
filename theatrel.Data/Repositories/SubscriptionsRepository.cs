using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Filters;

namespace theatrel.DataAccess.Repositories
{
    internal class SubscriptionsRepository : ISubscriptionsRepository
    {
        private readonly AppDbContext _dbContext;
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
                return _dbContext.Subscriptions.Include(s => s.PerformanceFilter).AsNoTracking();
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"GetList subscription with filter DbException {ex.Message}");
            }

            return new SubscriptionEntity[0];
        }

        private Task<SubscriptionEntity> GetById(int id)
            => _dbContext.Subscriptions.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);

        public async Task<SubscriptionEntity> Create(long userId, IPerformanceFilter filter, CancellationToken cancellationToken)
        {
            try
            {
                PerformanceFilterEntity filterEntity = AddFilter(filter);
                TelegramUserEntity userEntity = await _dbContext.TlUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);

                SubscriptionEntity entity = new SubscriptionEntity
                {
                    TelegramUser = userEntity,
                    PerformanceFilter = filterEntity,
                    LastUpdate = DateTime.Now

                };

                _dbContext.Subscriptions.Add(entity);
                _dbContext.Add(filterEntity);

                await _dbContext.SaveChangesAsync(cancellationToken);
                return entity;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"Create subscription DbException {ex.Message} inner exception {ex.InnerException?.Message}");
                return null;
            }
        }

        public async Task<bool> Delete(SubscriptionEntity entity)
        {
            _dbContext.Subscriptions.Remove(entity);

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

        public async Task<bool> Update(SubscriptionEntity newValue)
        {
            SubscriptionEntity oldValue = await GetById(newValue.Id);

            if (oldValue == null)
                return false;

            _dbContext.Entry(newValue).State = EntityState.Modified;

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

        private PerformanceFilterEntity AddFilter(IPerformanceFilter filterData)
        {
            try
            {
                var filter = new PerformanceFilterEntity
                {
                    StartDate = filterData.StartDate,
                    EndDate = filterData.EndDate,
                    DaysOfWeek = filterData.DaysOfWeek,
                    Locations = filterData.Locations,
                    PerformanceTypes = filterData.PerformanceTypes,
                    PartOfDay = filterData.PartOfDay
                };

                _dbContext.Filters.Add(filter);

                return filter;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to AddFilter {ex.Message}, inner exception {ex.InnerException?.Message}");
                return null;
            }
        }


        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }

}
