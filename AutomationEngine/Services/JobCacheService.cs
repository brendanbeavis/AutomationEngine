using AutomationEngine.Data.Entities;
using AutomationEngine.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services
{
    /// <summary>
    /// In-memory cache service for jobs
    /// Single Responsibility: Cache management only
    /// Thread-safe dictionary with lock-based synchronization
    /// </summary>
    public class JobCacheService : IJobCache
    {
        private Dictionary<string, JobEntity> _cache = new();
        private readonly object _lockObject = new object();
        private readonly ILogger<JobCacheService> _logger;

        public JobCacheService(ILogger<JobCacheService> logger)
        {
            _logger = logger;
        }

        public async Task LoadAsync(List<JobEntity> jobs)
        {
            try
            {
                lock (_lockObject)
                {
                    _cache.Clear();
                    foreach (var job in jobs)
                    {
                        _cache[job.JobId] = job;
                    }
                }

                _logger.LogInformation("Cache loaded with {Count} jobs", jobs.Count);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load cache");
                throw;
            }
        }

        public List<JobEntity> GetAll()
        {
            lock (_lockObject)
            {
                return _cache.Values.ToList();
            }
        }

        public JobEntity? Get(string jobId)
        {
            lock (_lockObject)
            {
                _cache.TryGetValue(jobId, out var job);
                return job;
            }
        }

        public void Set(string jobId, JobEntity job)
        {
            lock (_lockObject)
            {
                _cache[jobId] = job;
            }
        }

        public void Remove(string jobId)
        {
            lock (_lockObject)
            {
                if (_cache.Remove(jobId))
                {
                    _logger.LogDebug("Cache entry removed for JobId: {JobId}", jobId);
                }
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                var count = _cache.Count;
                _cache.Clear();
                _logger.LogInformation("Cache cleared ({Count} entries removed)", count);
            }
        }

        public bool Contains(string jobId)
        {
            lock (_lockObject)
            {
                return _cache.ContainsKey(jobId);
            }
        }

        public int Count()
        {
            lock (_lockObject)
            {
                return _cache.Count;
            }
        }

        public bool TryGetValue(string jobId, out JobEntity? job)
        {
            lock (_lockObject)
            {
                return _cache.TryGetValue(jobId, out job);
            }
        }
    }
}

