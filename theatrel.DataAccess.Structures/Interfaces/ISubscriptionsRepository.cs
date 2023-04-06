﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Filters;

namespace theatrel.DataAccess.Structures.Interfaces;

public interface ISubscriptionsRepository : IDIRegistrable, IDisposable
{
    Task<SubscriptionEntity> Get(int id);
    IEnumerable<SubscriptionEntity> GetAllWithFilter();
    IEnumerable<VkSubscriptionEntity> GetAllWithFilterVk();

    SubscriptionEntity[] GetUserSubscriptions(long userId);

    IEnumerable<SubscriptionEntity> GetOutdatedList();

    Task<SubscriptionEntity> Create(long userId, int reasonOfChange, IPerformanceFilter filter,
        CancellationToken cancellationToken);

    Task<bool> Delete(SubscriptionEntity entity);
    public Task<bool> DeleteFilter(PerformanceFilterEntity entity);
    Task<bool> DeleteRange(IEnumerable<SubscriptionEntity> entities);
    Task<bool> UpdateDate(int id);

    PlaybillChangeEntity[] GetFreshChanges(DateTime lastUpdate);
}