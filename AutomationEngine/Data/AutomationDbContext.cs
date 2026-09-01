using Microsoft.EntityFrameworkCore;
using AutomationEngine.Data.Entities;

namespace AutomationEngine.Data
{
    public class AutomationDbContext : DbContext
    {
        public AutomationDbContext(DbContextOptions<AutomationDbContext> options) : base(options)
        {
        }

        public DbSet<JobEntity> Jobs { get; set; } = null!;
        public DbSet<JobRunEntity> JobRuns { get; set; } = null!;
        public DbSet<AppSettingsEntity> AppSettings { get; set; } = null!;
        public DbSet<AuditLogEntity> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relationship: Job has many JobRuns
            modelBuilder.Entity<JobEntity>()
                .HasMany(j => j.Runs)
                .WithOne(r => r.Job)
                .HasForeignKey(r => r.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for JobId lookup
            modelBuilder.Entity<JobEntity>()
                .HasIndex(j => j.JobId)
                .IsUnique();

            // Index for JobRuns lookup by JobId
            modelBuilder.Entity<JobRunEntity>()
                .HasIndex(r => r.JobId);

            // AppSettings is a singleton - only one record should exist
            modelBuilder.Entity<AppSettingsEntity>()
                .HasKey(a => a.Id);

            // AuditLog indexes for efficient querying
            modelBuilder.Entity<AuditLogEntity>()
                .HasIndex(a => a.JobId);

            modelBuilder.Entity<AuditLogEntity>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<AuditLogEntity>()
                .HasIndex(a => new { a.JobId, a.Timestamp })
                .IsDescending(false, true); // Timestamp descending for recent first
        }
    }
}
