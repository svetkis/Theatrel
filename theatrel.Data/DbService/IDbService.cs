using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Autofac;

namespace theatrel.DataAccess.DbService
{
    public interface IDbService : IDISingleton
    {
        AppDbContext GetDbContext();
        ITgChatsRepository GetChatsRepository();
        ITgUsersRepository GetUsersRepository();
        IPlaybillRepository GetPlaybillRepository();
        ISubscriptionsRepository GetSubscriptionRepository();

        Task MigrateDb(CancellationToken cancellationToken);
    }
}