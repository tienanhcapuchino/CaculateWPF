using Caculate.Configurations;
using Caculate.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Caculate.DataContext
{
    public class CaculateDbContext : DbContext
    {
        public CaculateDbContext(DbContextOptions<CaculateDbContext> options) : base(options)
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
            string databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaculateApp", "caculateapp.db");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath));
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }

        #region entities
        public DbSet<Member> Members { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderParticipant> OrderParticipants { get; set; }
        #endregion

    }
}
