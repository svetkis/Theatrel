using Microsoft.EntityFrameworkCore;
using theatrel.TLBot.Entities;

namespace theatrel.TLBot
{
    public class ApplicationContext : DbContext
    {
        public DbSet<TlUser> TlUsers { get; set; }

        public ApplicationContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(ThSettings.Config.DatabaseUrl);
        }
    }
}
