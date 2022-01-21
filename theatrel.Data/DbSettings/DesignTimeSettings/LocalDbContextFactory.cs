using Microsoft.EntityFrameworkCore.Design;

namespace theatrel.DataAccess.DbSettings.DesignTimeSettings;

public class LocalDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new AppDbContext(LocalDbContextOptionsFactory.Get());
}