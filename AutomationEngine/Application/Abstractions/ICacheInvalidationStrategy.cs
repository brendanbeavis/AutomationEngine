namespace AutomationEngine.Application.Abstractions
{
    public class CacheOperationResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public CacheInconsistency? Inconsistency { get; set; }
        public OperationType OperationType { get; set; }
    }

    public class CacheInconsistency
    {
        public string JobId { get; set; } = "";
        public string Description { get; set; } = "";
        public InconsistencyType Type { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    public enum OperationType
    {
        Update,
        Invalidate,
        Refresh,
        Remove
    }

    public enum InconsistencyType
    {
        StaleData,
        MissingEntry,
        ExtraEntry,
        DataMismatch,
        UnknownState
    }

    /// <summary>
    /// Interface for cache invalidation strategy
    /// Manages cache invalidation and consistency with database
    /// </summary>
    public interface ICacheInvalidationStrategy
    {
        /// <summary>
        /// Attempt to update cache with database changes, invalidating on failure
        /// </summary>
        CacheOperationResult AttemptCacheUpdate<T>(
            string jobId,
            T dbEntity,
            Action<T> updateCache,
            Action invalidateCache);

        /// <summary>
        /// Invalidate a cache entry
        /// </summary>
        CacheOperationResult InvalidateCacheEntry(
            string jobId,
            Action invalidateAction);

        /// <summary>
        /// Remove multiple cache entries
        /// </summary>
        CacheOperationResult RemoveCacheEntries(
            IEnumerable<string> jobIds,
            Action<string> removeAction);

        /// <summary>
        /// Check cache consistency
        /// </summary>
        CacheInconsistency? DetectCacheInconsistency(
            string jobId,
            DateTime? cachedLastUpdated,
            DateTime? dbLastUpdated,
            DateTime? cacheLoadedAt);

        /// <summary>
        /// Get a description of the cache operation result for logging
        /// </summary>
        string GetResultSummary(CacheOperationResult result);
    }
}
