using Microsoft.EntityFrameworkCore;
using System;
using theatrel.DataAccess;
using theatrel.DataAccess.DbSettings;

namespace theatrel.Subscriptions.Tests.TestSettings;

internal class TestDbContextOptionsFactory : IDbContextOptionsFactory
{
    public DbContextOptions<AppDbContext> Get()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        TestDbContextConfigurator.Configure(builder);

        return builder.Options;
    }
}

internal static class TestDbContextConfigurator
{
    private static readonly string randomValue = new Random().Next().ToString();
    public static void Configure(DbContextOptionsBuilder<AppDbContext> builder)
        => builder.UseInMemoryDatabase(randomValue);
}