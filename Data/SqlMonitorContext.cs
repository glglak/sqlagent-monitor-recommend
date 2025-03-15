using Microsoft.EntityFrameworkCore;
using SqlMonitor.Models;
using System;

namespace SqlMonitor.Data
{
    public class SqlMonitorContext : DbContext
    {
        public SqlMonitorContext(DbContextOptions<SqlMonitorContext> options)
            : base(options)
        {
        }

        public DbSet<DatabaseInfo> Databases { get; set; } = null!;
        public DbSet<PerformanceMetricsEntity> PerformanceMetrics { get; set; } = null!;
        public DbSet<SlowQueryEntity> SlowQueries { get; set; } = null!;
        public DbSet<IndexInfoEntity> IndexInfo { get; set; } = null!;
        public DbSet<MissingIndexEntity> MissingIndexes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entities
            modelBuilder.Entity<DatabaseInfo>(entity =>
            {
                entity.ToTable("Databases");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<PerformanceMetricsEntity>(entity =>
            {
                entity.ToTable("PerformanceMetrics");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<SlowQueryEntity>(entity =>
            {
                entity.ToTable("SlowQueries");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<IndexInfoEntity>(entity =>
            {
                entity.ToTable("IndexInfo");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<MissingIndexEntity>(entity =>
            {
                entity.ToTable("MissingIndexes");
                entity.HasKey(e => e.Id);
            });

            // Seed data if needed
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Example seed data
            modelBuilder.Entity<DatabaseInfo>().HasData(
                new DatabaseInfo { Id = "1", Name = "AdventureWorks", Server = "localhost", Status = "Online" }
            );
        }
    }

    // Entity classes with nullable properties
    public class PerformanceMetricsEntity
    {
        public int Id { get; set; }
        public string? DatabaseName { get; set; }
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskIOUsage { get; set; }
        public double NetworkIOUsage { get; set; }
    }

    public class SlowQueryEntity
    {
        public int Id { get; set; }
        public string? DatabaseName { get; set; }
        public string? QueryText { get; set; }
        public double ExecutionTime { get; set; }
        public double CpuTime { get; set; }
        public int LogicalReads { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public string? OptimizedQuery { get; set; }
        public bool Fixed { get; set; }
    }

    public class IndexInfoEntity
    {
        public int Id { get; set; }
        public string? DatabaseName { get; set; }
        public string? SchemaName { get; set; }
        public string? TableName { get; set; }
        public string? IndexName { get; set; }
        public double FragmentationPercentage { get; set; }
        public int PageCount { get; set; }
        public bool IsReindexed { get; set; }
        public DateTime LastReindexed { get; set; }
    }

    public class MissingIndexEntity
    {
        public int Id { get; set; }
        public string? DatabaseName { get; set; }
        public string? TableName { get; set; }
        public string? Columns { get; set; }
        public string? IncludeColumns { get; set; }
        public string? EstimatedImpact { get; set; }
        public int ImprovementPercent { get; set; }
        public string? CreateStatement { get; set; }
        public bool Created { get; set; }
    }
} 