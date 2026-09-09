using AutomationEngine.Data.Entities;
using AutomationEngine.Models;
using AutomationEngine.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Service for querying job data from cache
    /// Single Responsibility: Query operations only
    /// No state changes, no database writes
    /// </summary>
    public class JobQueryService : IJobQueryService
    {
        private readonly IJobCache _cache;
        private readonly ILogger<JobQueryService> _logger;

        public JobQueryService(IJobCache cache, ILogger<JobQueryService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public List<JobEntity> GetAllJobs()
        {
            var jobs = _cache.GetAll();
            _logger.LogDebug("Retrieved all jobs from cache | Count: {JobCount}", jobs.Count);
            return jobs;
        }

        public JobEntity? GetJobById(string jobId)
        {
            return _cache.Get(jobId);
        }

        public List<JobEntity> GetRunningJobsByType(JobType jobType)
        {
            var jobs = _cache.GetAll()
                .Where(j => j.Type == jobType && j.CurrentState == JobState.Running)
                .ToList();
            if (jobs.Any())
            {
                _logger.LogDebug("Found {RunningJobCount} running jobs of type {JobType}", jobs.Count, jobType);
            }
            return jobs;
        }

        public bool IsJobBusy(int id)
        {
            return _cache.GetAll()
                .Any(j => j.Id == id && j.CurrentState == JobState.Running);
        }

        public List<JobEntity> GetAllRunningJobs()
        {
            var jobs = _cache.GetAll()
                .Where(j => j.CurrentState == JobState.Running)
                .ToList();
            _logger.LogDebug("Retrieved {RunningJobCount} running jobs from cache", jobs.Count);
            return jobs;
        }

        public TimeSpan? GetJobRunningDuration(string jobId)
        {
            var job = _cache.Get(jobId);
            if (job != null && 
                job.CurrentState == JobState.Running && 
                job.RunningStartTime.HasValue)
            {
                return DateTime.UtcNow - job.RunningStartTime.Value;
            }
            return null;
        }

        public List<JobEntity> GetJobsByEnabledStatus(bool enabled)
        {
            return _cache.GetAll()
                .Where(j => j.Enabled == enabled)
                .ToList();
        }

        public List<JobEntity> GetJobsByType(JobType jobType)
        {
            var jobs = _cache.GetAll()
                .Where(j => j.Type == jobType)
                .ToList();
            _logger.LogDebug("Retrieved {JobCount} jobs of type {JobType}", jobs.Count, jobType);
            return jobs;
        }
    }
}

