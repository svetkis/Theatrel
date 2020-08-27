using theatrel.DataAccess;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Autofac;

namespace theatrel.Core.DataAccess
{
    public interface IDbService : IDISingleton
    {
        AppDbContext GetDbContext();
        ITgChatsRepository GetChatsRepository();
    }

    public class DbService : IDbService
    {
        private readonly IDbContextOptionsFactory _optionsFactory;
        public DbService(IDbContextOptionsFactory optionsFactory)
        {
            _optionsFactory = optionsFactory;
        }

        public AppDbContext GetDbContext() => new AppDbContext(_optionsFactory.Get());

        public ITgChatsRepository GetChatsRepository()
        {
            return new TgChatsRepository(GetDbContext());
        }
    }
}
