using Caculate.Configurations;
using Caculate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Caculate.DataContext
{
    public class CaculateDbContext : DbContext
    {
        public CaculateDbContext(DbContextOptions<CaculateDbContext> options) : base(options)
        {
        }

        public CaculateDbContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new MemberConfiguration());
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new OrderParticipantConfiguration());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var conStr = config.GetConnectionString("CaculateConnectionStr");
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(conStr);
            }
        }

        #region entities
        public DbSet<Member> Members { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderParticipant> OrderParticipants { get; set; }
        #endregion

    }
}
