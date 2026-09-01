using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutomationEngine.Data;
using AutomationEngine.Data.Entities;
using AutomationEngine.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Service for managing audit logs and retention policies.
    /// All significant job activities are logged for compliance, debugging, and audit purposes.
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly ILogger<AuditLogService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AuditLogService(ILogger<AuditLogService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Log an audit event for a job
        /// </summary>
        public async Task LogAuditEventAsync(
            int jobId,
            string action,
            string? description = null,
            bool? success = null,
            int? exitCode = null,
            int? durationMs = null,
            string? errorMessage = null,
            object? details = null)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                    var auditLog = new AuditLogEntity
                    {
                        JobId = jobId,
                        Action = action,
                        Timestamp = DateTime.UtcNow,
                        Description = description,
                        Success = success,
                        ExitCode = exitCode,
                        DurationMs = durationMs,
                        ErrorMessage = errorMessage,
                        Details = details != null ? JsonSerializer.Serialize(details) : null
                    };

                    db.AuditLogs.Add(auditLog);
                    await db.SaveChangesAsync();

                    _logger.LogInformation(
                        "Audit log created: Job {JobId}, Action: {Action}, Description: {Description}",
                        jobId, action, description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log for job {JobId}, action {Action}", jobId, action);
                // Don't throw - audit logging failure shouldn't break the app
            }
        }

        /// <summary>
        /// Get all audit logs for a specific job
        /// </summary>
        public async Task<List<AuditLogEntity>> GetJobAuditLogsAsync(int jobId, int limit = 100)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                return await db.AuditLogs
                    .Where(a => a.JobId == jobId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Get all audit logs for multiple jobs
        /// </summary>
        public async Task<List<AuditLogEntity>> GetMultipleJobsAuditLogsAsync(IEnumerable<int> jobIds, int limit = 100)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
                var jobIdList = jobIds.ToList();

                return await db.AuditLogs
                    .Where(a => jobIdList.Contains(a.JobId))
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Get recent audit logs globally
        /// </summary>
        public async Task<List<AuditLogEntity>> GetRecentAuditLogsAsync(int limit = 50)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                return await db.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Get audit logs by action type
        /// </summary>
        public async Task<List<AuditLogEntity>> GetAuditLogsByActionAsync(string action, int limit = 100)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                return await db.AuditLogs
                    .Where(a => a.Action == action)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Clean up old audit logs (retention policy)
        /// Keeps audit logs for 1 year (365 days) by default
        /// </summary>
        public async Task<int> CleanupOldAuditLogsAsync(int retentionDays = 365)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                    var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                    var oldLogs = await db.AuditLogs
                        .Where(a => a.Timestamp < cutoffDate)
                        .ToListAsync();

                    if (oldLogs.Count == 0)
                    {
                        _logger.LogInformation("No audit logs older than {Days} days to cleanup", retentionDays);
                        return 0;
                    }

                    db.AuditLogs.RemoveRange(oldLogs);
                    await db.SaveChangesAsync();

                    _logger.LogInformation(
                        "Cleaned up {Count} audit logs older than {Days} days (before {Date})",
                        oldLogs.Count, retentionDays, cutoffDate);

                    return oldLogs.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old audit logs");
                return 0;
            }
        }

        /// <summary>
        /// Get audit log statistics (e.g., for dashboard)
        /// </summary>
        public async Task<AuditLogStatistics> GetAuditLogStatisticsAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                var stats = new AuditLogStatistics
                {
                    TotalAuditLogs = await db.AuditLogs.CountAsync(),
                    JobsCreatedToday = await db.AuditLogs
                        .Where(a => a.Action == AuditLogActions.JobCreated && 
                                   a.Timestamp >= DateTime.UtcNow.AddDays(-1))
                        .CountAsync(),
                    JobsDeletedToday = await db.AuditLogs
                        .Where(a => a.Action == AuditLogActions.JobDeleted && 
                                   a.Timestamp >= DateTime.UtcNow.AddDays(-1))
                        .CountAsync(),
                    JobsExecutedToday = await db.AuditLogs
                        .Where(a => a.Action == AuditLogActions.JobCompleted && 
                                   a.Timestamp >= DateTime.UtcNow.AddDays(-1))
                        .CountAsync(),
                    JobsFailedToday = await db.AuditLogs
                        .Where(a => a.Action == AuditLogActions.JobFailed && 
                                   a.Timestamp >= DateTime.UtcNow.AddDays(-1))
                        .CountAsync(),
                    OldestAuditLog = await db.AuditLogs
                        .OrderBy(a => a.Timestamp)
                        .Select(a => a.Timestamp)
                        .FirstOrDefaultAsync(),
                    NewestAuditLog = await db.AuditLogs
                        .OrderByDescending(a => a.Timestamp)
                        .Select(a => a.Timestamp)
                        .FirstOrDefaultAsync()
                };

                return stats;
            }
        }
    }

    /// <summary>
    /// Audit log statistics for dashboard/reporting
    /// </summary>
    public class AuditLogStatistics
    {
        public int TotalAuditLogs { get; set; }
        public int JobsCreatedToday { get; set; }
        public int JobsDeletedToday { get; set; }
        public int JobsExecutedToday { get; set; }
        public int JobsFailedToday { get; set; }
        public DateTime? OldestAuditLog { get; set; }
        public DateTime? NewestAuditLog { get; set; }
    }
}
