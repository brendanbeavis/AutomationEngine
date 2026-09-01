using AutomationEngine.Data.Entities;
using AutomationEngine.Dto;

namespace AutomationEngine.Services.Abstractions
{
    /// <summary>
    /// Repository interface for job data persistence operations
    /// Responsible for all database CRUD operations related to jobs
    /// Single Responsibility: Database access and persistence only
    /// </summary>
    public interface IJobRepository
    {
        /// <summary>
        /// Load all active (non-deleted) jobs from database
        /// </summary>
        Task<List<JobEntity>> LoadAllActiveJobsAsync();

        /// <summary>
        /// Get a job by ID from database
        /// </summary>
        Task<JobEntity?> GetJobByIdAsync(string jobId);

        /// <summary>
        /// Get a job by entity ID from database
        /// </summary>
        Task<JobEntity?> GetJobByEntityIdAsync(int jobEntityId);

        /// <summary>
        /// Create a new job in database
        /// </summary>
        Task<JobEntity> CreateJobAsync(JobEntity job);

        /// <summary>
        /// Update an existing job in database
        /// </summary>
        Task<JobEntity> UpdateJobAsync(JobEntity job);

        /// <summary>
        /// Soft-delete a job (mark as deleted but keep in DB)
        /// </summary>
        Task DeleteJobAsync(string jobId);

        /// <summary>
        /// Get recent job runs for a specific job
        /// </summary>
        Task<List<JobRunEntity>> GetJobRunsAsync(string jobId, int limit = 50);

        /// <summary>
        /// Save a job run result to database
        /// </summary>
        Task SaveJobRunAsync(JobRunEntity run);

        /// <summary>
        /// Get all jobs (including deleted) - for admin/reporting
        /// </summary>
        Task<List<JobEntity>> GetAllJobsIncludingDeletedAsync();

        /// <summary>
        /// Check if job exists by ID
        /// </summary>
        Task<bool> JobExistsAsync(string jobId);
    }
}
