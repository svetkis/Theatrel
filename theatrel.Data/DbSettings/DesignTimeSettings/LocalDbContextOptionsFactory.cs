using Microsoft.EntityFrameworkCore;

namespace theatrel.DataAccess.DbSettings.DesignTimeSettings;

public static class LocalDbContextOptionsFactory
{
    public static DbContextOptions<AppDbContext> Get()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();

        DesignTimeDbContextConfigurator.Configure(builder);

        return builder.Options;
    }
}