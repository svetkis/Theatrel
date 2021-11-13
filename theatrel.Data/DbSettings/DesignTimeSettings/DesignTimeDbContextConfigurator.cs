using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace theatrel.DataAccess.DbSettings.DesignTimeSettings
{
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
                Database = "theatrel"
            }.ToString();
        }
    }
}