using Microsoft.EntityFrameworkCore;

namespace theatrel.DataAccess.DbSettings;

internal class DbContextOptionsFactory : IDbContextOptionsFactory
{
    public DbContextOptions<AppDbContext> Get()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        DbContextConfigurator.Configure(builder);

        return builder.Options;
    }
}