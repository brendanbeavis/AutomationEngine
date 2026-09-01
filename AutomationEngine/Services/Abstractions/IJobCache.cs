using AutomationEngine.Data.Entities;

namespace AutomationEngine.Services.Abstractions
{
    /// <summary>
    /// Cache interface for in-memory job storage
    /// Single Responsibility: Cache management only
    /// Does not perform database operations or business logic
    /// </summary>
    public interface IJobCache
    {
        /// <summary>
        /// Load all jobs from source into cache
        /// </summary>
        Task LoadAsync(List<JobEntity> jobs);

        /// <summary>
        /// Get all jobs from cache
        /// </summary>
        List<JobEntity> GetAll();

        /// <summary>
        /// Get a specific job from cache by ID
        /// </summary>
        JobEntity? Get(string jobId);

        /// <summary>
        /// Add or update a job in cache
        /// </summary>
        void Set(string jobId, JobEntity job);

        /// <summary>
        /// Remove a job from cache
        /// </summary>
        void Remove(string jobId);

        /// <summary>
        /// Clear the entire cache
        /// </summary>
        void Clear();

        /// <summary>
        /// Check if a job exists in cache
        /// </summary>
        bool Contains(string jobId);

        /// <summary>
        /// Get the count of jobs in cache
        /// </summary>
        int Count();

        /// <summary>
        /// Try to get a job from cache safely
        /// </summary>
        bool TryGetValue(string jobId, out JobEntity? job);
    }
}
