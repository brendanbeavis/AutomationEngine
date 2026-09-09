using AutomationEngine.Data.Entities;
using AutomationEngine.Models;

namespace AutomationEngine.Application.Abstractions
{
    /// <summary>
    /// Service for querying jobs with business logic
    /// Single Responsibility: Query operations and predicates only
    /// Does not modify state or persistence
    /// </summary>
    public interface IJobQueryService
    {
        /// <summary>
        /// Get all jobs currently in memory cache
        /// </summary>
        List<JobEntity> GetAllJobs();

        /// <summary>
        /// Get a specific job by ID from cache
        /// </summary>
        JobEntity? GetJobById(string jobId);

        /// <summary>
        /// Get all jobs of a specific type that are currently running
        /// </summary>
        List<JobEntity> GetRunningJobsByType(JobType jobType);

        /// <summary>
        /// Check if a job is currently running
        /// </summary>
        bool IsJobBusy(int id);

        /// <summary>
        /// Get all currently running jobs
        /// </summary>
        List<JobEntity> GetAllRunningJobs();

        /// <summary>
        /// Get a job's current running duration (if running)
        /// </summary>
        TimeSpan? GetJobRunningDuration(string jobId);

        /// <summary>
        /// Get all jobs with a specific enabled status
        /// </summary>
        List<JobEntity> GetJobsByEnabledStatus(bool enabled);

        /// <summary>
        /// Get jobs by type
        /// </summary>
        List<JobEntity> GetJobsByType(JobType jobType);
    }
}
