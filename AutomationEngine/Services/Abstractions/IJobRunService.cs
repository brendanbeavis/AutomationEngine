using AutomationEngine.Data.Entities;
using AutomationEngine.Services;

namespace AutomationEngine.Services.Abstractions
{
    /// <summary>
    /// Service for managing job run results and history
    /// Single Responsibility: Job runs tracking and persistence
    /// Handles creation and retrieval of job execution records
    /// </summary>
    public interface IJobRunService
    {
        /// <summary>
        /// Save a job run result to database and update job state
        /// </summary>
        Task SaveJobRunAsync(string jobId, JobResult result, DateTime startedAt, TimeSpan duration);

        /// <summary>
        /// Get recent job runs for a specific job
        /// </summary>
        Task<List<JobRunEntity>> GetJobRunsAsync(string jobId, int limit = 50);

        /// <summary>
        /// Get a specific job run by ID
        /// </summary>
        Task<JobRunEntity?> GetJobRunByIdAsync(int runId);

        /// <summary>
        /// Get job run statistics
        /// </summary>
        Task<JobRunStatistics> GetJobRunStatisticsAsync(string jobId);

        /// <summary>
        /// Delete old job runs (retention policy)
        /// </summary>
        Task<int> DeleteOldJobRunsAsync(int retentionDays = 90);
    }

    /// <summary>
    /// Statistics about job runs
    /// </summary>
    public class JobRunStatistics
    {
        public int TotalRuns { get; set; }
        public int SuccessfulRuns { get; set; }
        public int FailedRuns { get; set; }
        public TimeSpan? AverageDuration { get; set; }
        public DateTime? LastRunTime { get; set; }
        public bool LastRunSuccess { get; set; }
    }
}
