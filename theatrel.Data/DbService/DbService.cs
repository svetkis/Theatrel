using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbSettings;
using theatrel.DataAccess.Repositories;
using theatrel.DataAccess.Structures.Interfaces;

namespace theatrel.DataAccess.DbService
{
    public class DbService : IDbService
    {
        private readonly IDbContextOptionsFactory _optionsFactory;
        public DbService(IDbContextOptionsFactory optionsFactory)
        {
            _optionsFactory = optionsFactory;
        }

        public AppDbContext GetDbContext() => new(_optionsFactory.Get());

        public ITgChatsRepository GetChatsRepository() => new TgChatsRepository(GetDbContext());
        public ITgUsersRepository GetUsersRepository() => new TgUsersRepository(GetDbContext());

        public IPlaybillRepository GetPlaybillRepository() => new PlaybillRepository(GetDbContext());

        public ISubscriptionsRepository GetSubscriptionRepository() => new SubscriptionsRepository(GetDbContext());
        public async Task MigrateDb(CancellationToken cancellationToken)
        {
            await using var db = GetDbContext();
            db.Database.Migrate();
        }
    }
}
