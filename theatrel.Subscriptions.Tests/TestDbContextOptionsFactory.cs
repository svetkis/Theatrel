using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess;

namespace theatrel.Subscriptions.Tests
{
    public class TestDbContextOptionsFactory
    {
        public static DbContextOptions<AppDbContext> Get()
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            TestDbContextConfigurator.Configure(builder);

            return builder.Options;
        }
    }

    public class TestDbContextConfigurator
    {
        public static void Configure(DbContextOptionsBuilder<AppDbContext> builder)
            => builder.UseInMemoryDatabase(databaseName: "SubscriptionsTest");
    }
}
