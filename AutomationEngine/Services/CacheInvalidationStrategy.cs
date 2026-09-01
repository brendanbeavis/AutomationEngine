using Microsoft.Extensions.Logging;
using AutomationEngine.Services.Abstractions;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Strategy for managing cache invalidation and consistency with database.
    /// Provides patterns for safe cache updates with proper transaction semantics.
    /// </summary>
    public class CacheInvalidationStrategy : ICacheInvalidationStrategy
    {
        private readonly ILogger _logger;

        public CacheInvalidationStrategy(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Represents the result of a cache operation
        /// </summary>
        public class CacheOperationResult
        {
            public bool Success { get; set; }
            public string? Error { get; set; }
            public CacheInconsistency? Inconsistency { get; set; }
            public OperationType OperationType { get; set; }
        }

        /// <summary>
        /// Represents detected cache inconsistency
        /// </summary>
        public class CacheInconsistency
        {
            public string JobId { get; set; } = "";
            public string Description { get; set; } = "";
            public InconsistencyType Type { get; set; }
            public DateTime DetectedAt { get; set; }
        }

        /// <summary>
        /// Types of cache operations
        /// </summary>
        public enum OperationType
        {
            Update,
            Invalidate,
            Refresh,
            Remove
        }

        /// <summary>
        /// Types of detected inconsistencies
        /// </summary>
        public enum InconsistencyType
        {
            StaleData,           // Cache data is outdated
            MissingEntry,        // Entry should exist in cache but doesn't
            ExtraEntry,          // Entry exists in cache but not in DB
            DataMismatch,        // Cached data differs from DB
            UnknownState         // Unable to determine consistency
        }

        /// <summary>
        /// Attempt to update cache with database changes, invalidating on failure
        /// This provides transaction-like semantics: if update fails, cache is marked invalid
        /// </summary>
        public CacheOperationResult AttemptCacheUpdate<T>(
            string jobId,
            T dbEntity,
            Action<T> updateCache,
            Action invalidateCache)
        {
            try
            {
                // Attempt to update cache with latest data from database
                updateCache(dbEntity);

                _logger.LogDebug("Cache updated successfully for JobId: {JobId}", jobId);

                return new CacheOperationResult
                {
                    Success = true,
                    OperationType = OperationType.Update
                };
            }
            catch (Exception ex)
            {
                // If cache update fails, we must invalidate to maintain consistency
                _logger.LogWarning(ex, "Cache update failed for JobId: {JobId}, invalidating cache to maintain consistency", jobId);

                try
                {
                    invalidateCache();
                    _logger.LogInformation("Cache invalidated for JobId: {JobId} due to update failure", jobId);
                }
                catch (Exception invEx)
                {
                    _logger.LogError(invEx, "Failed to invalidate cache for JobId: {JobId} after update failure", jobId);
                }

                return new CacheOperationResult
                {
                    Success = false,
                    Error = ex.Message,
                    Inconsistency = new CacheInconsistency
                    {
                        JobId = jobId,
                        Description = "Cache update failed, entry invalidated",
                        Type = InconsistencyType.UnknownState,
                        DetectedAt = DateTime.UtcNow
                    },
                    OperationType = OperationType.Update
                };
            }
        }

        /// <summary>
        /// Invalidate a cache entry (remove it)
        /// </summary>
        public CacheOperationResult InvalidateCacheEntry(
            string jobId,
            Action invalidateAction)
        {
            try
            {
                invalidateAction();
                _logger.LogInformation("Cache entry invalidated for JobId: {JobId}", jobId);

                return new CacheOperationResult
                {
                    Success = true,
                    OperationType = OperationType.Invalidate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate cache entry for JobId: {JobId}", jobId);

                return new CacheOperationResult
                {
                    Success = false,
                    Error = ex.Message,
                    OperationType = OperationType.Invalidate
                };
            }
        }

        /// <summary>
        /// Remove multiple cache entries
        /// </summary>
        public CacheOperationResult RemoveCacheEntries(
            IEnumerable<string> jobIds,
            Action<string> removeAction)
        {
            var errors = new List<string>();

            foreach (var jobId in jobIds)
            {
                try
                {
                    removeAction(jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove cache entry for JobId: {JobId}", jobId);
                    errors.Add($"{jobId}: {ex.Message}");
                }
            }

            if (errors.Any())
            {
                _logger.LogError("Failed to remove {Count} cache entries: {Errors}", 
                    errors.Count, string.Join("; ", errors));

                return new CacheOperationResult
                {
                    Success = false,
                    Error = string.Join("; ", errors),
                    OperationType = OperationType.Remove
                };
            }

            _logger.LogInformation("Successfully removed {Count} cache entries", jobIds.Count());

            return new CacheOperationResult
            {
                Success = true,
                OperationType = OperationType.Remove
            };
        }

        /// <summary>
        /// Check cache consistency - if cache appears stale, it should be refreshed
        /// </summary>
        public CacheInconsistency? DetectCacheInconsistency(
            string jobId,
            DateTime? cachedLastUpdated,
            DateTime? dbLastUpdated,
            DateTime? cacheLoadedAt)
        {
            // If DB was updated after cache was loaded, cache is stale
            if (cacheLoadedAt.HasValue && dbLastUpdated.HasValue && dbLastUpdated > cacheLoadedAt)
            {
                return new CacheInconsistency
                {
                    JobId = jobId,
                    Description = $"Database updated ({dbLastUpdated:u}) after cache load ({cacheLoadedAt:u})",
                    Type = InconsistencyType.StaleData,
                    DetectedAt = DateTime.UtcNow
                };
            }

            // If cache timestamp differs significantly from DB, investigate
            if (cachedLastUpdated.HasValue && dbLastUpdated.HasValue)
            {
                var timeDiff = Math.Abs((cachedLastUpdated.Value - dbLastUpdated.Value).TotalSeconds);
                if (timeDiff > 5) // More than 5 seconds difference
                {
                    return new CacheInconsistency
                    {
                        JobId = jobId,
                        Description = $"Cached UpdatedAt ({cachedLastUpdated:u}) differs from DB ({dbLastUpdated:u}) by {timeDiff} seconds",
                        Type = InconsistencyType.DataMismatch,
                        DetectedAt = DateTime.UtcNow
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Get a description of the cache operation result for logging
        /// </summary>
        public string GetResultSummary(CacheOperationResult result)
        {
            return result.Success 
                ? $"Cache operation {result.OperationType} succeeded"
                : $"Cache operation {result.OperationType} failed: {result.Error}";
        }
    }
}
