using Microsoft.EntityFrameworkCore;
using SqlMonitor.Models;

namespace SqlMonitor.Data
{
    public class SqlMonitorContext : DbContext
    {
        public SqlMonitorContext(DbContextOptions<SqlMonitorContext> options)
            : base(options)
        {
        }

        public DbSet<SlowQueryHistory> SlowQueries { get; set; }
        public DbSet<IndexHistory> IndexOperations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SlowQueryHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QueryText).IsRequired();
                entity.Property(e => e.DatabaseName).IsRequired();
                entity.Property(e => e.FirstSeen).IsRequired();
                entity.Property(e => e.LastSeen).IsRequired();
                entity.HasIndex(e => e.LastSeen);
            });
        }
    }
} 