using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Autofac;

namespace theatrel.DataAccess.Structures.Interfaces
{
    public interface ISubscriptionsRepository : IDIRegistrable, IDisposable
    {
        Task<SubscriptionEntity> Get(int id);
        public IEnumerable<SubscriptionEntity> GetAllWithFilter();

        Task<SubscriptionEntity> Create(SubscriptionEntity entity, CancellationToken cancellationToken);

        Task<bool> Delete(SubscriptionEntity entity);
        Task<bool> Update(SubscriptionEntity newValue);
    }
}
