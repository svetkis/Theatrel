using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using theatrel.DataAccess.Structures.Entities;

namespace theatrel.DataAccess;

public class AppDbContext : DbContext
{
    public DbSet<TelegramUserEntity> TlUsers { get; set; } = null!;
    public DbSet<ChatInfoEntity> TlChats { get; set; } = null!;
    public DbSet<PerformanceEntity> Performances { get; set; } = null!;
    public DbSet<PlaybillChangeEntity> PlaybillChanges { get; set; } = null!;
    public DbSet<PerformanceFilterEntity> Filters { get; set; } = null!;
    public DbSet<SubscriptionEntity> Subscriptions { get; set; } = null!;
    public DbSet<PerformanceTypeEntity> PerformanceTypes { get; set; } = null!;
    public DbSet<LocationsEntity> PerformanceLocations { get; set; } = null!;
    public DbSet<PlaybillEntity> Playbill { get; set; } = null!;

    public DbSet<ActorInRoleEntity> ActorInRole { get; set; } = null!;
    public DbSet<ActorEntity> Actors { get; set; } = null!;
    public DbSet<RoleEntity> Roles { get; set; } = null!;

    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    private DateTime ConvertDateTime(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private DateTime ConvertDateTimeBack(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActorInRoleEntity>().HasKey(actorInRole => new { actorInRole.ActorId, actorInRole.RoleId, actorInRole.PlaybillId });

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(v => ConvertDateTime(v), v => ConvertDateTimeBack(v));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsKeyless)
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}