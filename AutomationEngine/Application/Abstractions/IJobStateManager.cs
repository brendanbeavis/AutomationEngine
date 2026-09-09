using AutomationEngine.Api.Requests;
using AutomationEngine.Data.Entities;
using AutomationEngine.Models;
using AutomationEngine.Services;

namespace AutomationEngine.Application.Abstractions
{
    /// <summary>
    /// Interface for job state management service
    /// Manages job state, caching, and persistence
    /// </summary>
    public interface IJobStateManager
    {
        /// <summary>
        /// Load all active (non-deleted) jobs from database into memory cache
        /// </summary>
        Task LoadJobsAsync();

        /// <summary>
        /// Refresh a specific job from database into cache
        /// </summary>
        Task<JobEntity?> RefreshJobAsync(string jobId);

        /// <summary>
        /// Get all cached jobs
        /// </summary>
        List<JobEntity> GetAllJobs();

        /// <summary>
        /// Get a specific job from cache
        /// </summary>
        JobEntity? GetJob(string jobId);

        /// <summary>
        /// Save a new job or update existing
        /// </summary>
        Task<JobEntity> SaveJobAsync(SaveJobRequest request);

        /// <summary>
        /// Delete a job (soft delete)
        /// </summary>
        Task DeleteJobAsync(string jobId);

        /// <summary>
        /// Save a job run result
        /// </summary>
        Task SaveJobRunAsync(string jobId, JobResult result, DateTime startedAt, TimeSpan duration);

        /// <summary>
        /// Get job runs for a specific job
        /// </summary>
        Task<List<JobRunEntity>> GetJobRunsAsync(string jobId, int limit = 50);

        /// <summary>
        /// Set job enabled/disabled status
        /// </summary>
        Task SetJobEnabledAsync(string jobId, bool enabled);

        /// <summary>
        /// Get all running jobs of a specific type
        /// </summary>
        List<JobEntity> GetRunningJobsByType(JobType jobType);

        /// <summary>
        /// Check if a job is currently busy (has running jobs)
        /// </summary>
        bool IsJobBusy(int id);

        /// <summary>
        /// Set job state
        /// </summary>
        Task SetJobStateAsync(string jobId, JobState newState, int? processId = null);

        /// <summary>
        /// Get all currently running jobs
        /// </summary>
        List<JobEntity> GetAllRunningJobs();

        /// <summary>
        /// Get running duration for a specific job
        /// </summary>
        TimeSpan? GetJobRunningDuration(string jobId);
    }
}
