using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess.Structures.Entities;

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
}
