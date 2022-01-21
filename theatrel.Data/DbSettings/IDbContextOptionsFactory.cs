using Microsoft.EntityFrameworkCore;
using theatrel.Interfaces.Autofac;

namespace theatrel.DataAccess.DbSettings;

public interface IDbContextOptionsFactory : IDIRegistrable
{
    DbContextOptions<AppDbContext> Get();
}