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

        public AppDbContext GetDbContext() => new AppDbContext(_optionsFactory.Get());

        public ITgChatsRepository GetChatsRepository() => new TgChatsRepository(GetDbContext());
        public ITgUsersRepository GetUsersRepository() => new TgUsersRepository(GetDbContext());
    }
}
