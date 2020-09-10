using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess;
using theatrel.DataAccess.DbSettings;

namespace theatrel.DataUpdater.Tests.TestSettings
{
    internal class TestDbContextOptionsFactory : IDbContextOptionsFactory
    {
        public DbContextOptions<AppDbContext> Get()
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            TestDbContextConfigurator.Configure(builder);

            return builder.Options;
        }
    }

    internal class TestDbContextConfigurator
    {
        public static void Configure(DbContextOptionsBuilder<AppDbContext> builder)
            => builder.UseInMemoryDatabase("UpdaterServiceTestDb");
    }
}
