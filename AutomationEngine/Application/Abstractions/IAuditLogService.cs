using AutomationEngine.Data.Entities;

namespace AutomationEngine.Application.Abstractions
{
    /// <summary>
    /// Interface for audit logging service
    /// Manages audit logs and retention policies for job activities
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Log an audit event for a job
        /// </summary>
        Task LogAuditEventAsync(
            int jobId,
            string action,
            string? description = null,
            bool? success = null,
            int? exitCode = null,
            int? durationMs = null,
            string? errorMessage = null,
            object? details = null);

        /// <summary>
        /// Get all audit logs for a specific job
        /// </summary>
        Task<List<AuditLogEntity>> GetJobAuditLogsAsync(int jobId, int limit = 100);

        /// <summary>
        /// Get all audit logs for multiple jobs
        /// </summary>
        Task<List<AuditLogEntity>> GetMultipleJobsAuditLogsAsync(IEnumerable<int> jobIds, int limit = 100);

        /// <summary>
        /// Get recent audit logs globally
        /// </summary>
        Task<List<AuditLogEntity>> GetRecentAuditLogsAsync(int limit = 50);

        /// <summary>
        /// Get audit logs by action type
        /// </summary>
        Task<List<AuditLogEntity>> GetAuditLogsByActionAsync(string action, int limit = 100);

        /// <summary>
        /// Clean up old audit logs (retention policy)
        /// </summary>
        Task<int> CleanupOldAuditLogsAsync(int retentionDays = 365);
    }
}
