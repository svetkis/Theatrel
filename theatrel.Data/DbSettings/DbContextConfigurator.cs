﻿using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;

namespace theatrel.DataAccess.DbSettings;

internal static class DbContextConfigurator
{
    public static void Configure(DbContextOptionsBuilder<AppDbContext> builder)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        builder.UseNpgsql(GetConnectionString());
    }

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
            Database = databaseUri.LocalPath.TrimStart('/'),
            //SslMode = SslMode.Require,

            TrustServerCertificate = true,
            KeepAlive = 300
        };

        return builder.ToString();
    }
}