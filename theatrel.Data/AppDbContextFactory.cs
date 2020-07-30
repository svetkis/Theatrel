using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace theatrel.DataAccess
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args) => new AppDbContext(TestDbContextOptionsFactory.Get());
    }

    public class TestDbContextOptionsFactory
    {
        public static DbContextOptions<AppDbContext> Get()
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();

            DesignTimeDbContextConfigurator.Configure(builder);

            return builder.Options;
        }
    }

    public class DesignTimeDbContextConfigurator
    {
        public static void Configure(DbContextOptionsBuilder<AppDbContext> builder)
            => builder.UseNpgsql(GetConnectionString());

        private static string GetConnectionString()
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Username = "postgres",
                Password = "",
                Database = "TheatrelTest"
            }.ToString();
        }
    }
}
