﻿using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using theatrel.DataAccess.Entities;

namespace theatrel.DataAccess
{
    public class AppDbContext : DbContext
    {
        public DbSet<TelegramUserEntity> TlUsers { get; set; } = null!;
        public DbSet<ChatInfoEntity> TlChats { get; set; } = null!;
        public DbSet<PerformanceEntity> Performances { get; set; } = null!;
        public DbSet<PlaybillChangeEntity> PerformanceChanges { get; set; } = null!;
        public DbSet<PerformanceFilterEntity> Filters { get; set; } = null!;
        public DbSet<SubscriptionEntity> Subscriptions { get; set; } = null!;
        public DbSet<PerformanceTypeEntity> PerformanceTypes { get; set; } = null!;
        public DbSet<LocationsEntity> PerformanceLocations { get; set; } = null!;
        public DbSet<PlaybillEntity> Playbill { get; set; } = null!;

        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
    }

    public class DbContextOptionsFactory
    {
        public static DbContextOptions<AppDbContext> Get()
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            DbContextConfigurator.Configure(builder);

            return builder.Options;
        }
    }

    public class DbContextConfigurator
    {
        public static void Configure(DbContextOptionsBuilder<AppDbContext> builder)
            => builder.UseNpgsql(GetConnectionString());

        private static string GetConnectionString()
        {
            var databaseUri = new Uri(Environment.GetEnvironmentVariable("DATABASE_URL"));
            var userInfo = databaseUri.UserInfo.Split(':');

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/')
            };

            return builder.ToString();
        }
    }
}
