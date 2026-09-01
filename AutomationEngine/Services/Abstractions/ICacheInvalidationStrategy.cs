namespace AutomationEngine.Services.Abstractions
{
    /// <summary>
    /// Interface for cache invalidation strategy
    /// Manages cache invalidation and consistency with database
    /// </summary>
    public interface ICacheInvalidationStrategy
    {
        /// <summary>
        /// Attempt to update cache with database changes, invalidating on failure
        /// </summary>
        CacheInvalidationStrategy.CacheOperationResult AttemptCacheUpdate<T>(
            string jobId,
            T dbEntity,
            Action<T> updateCache,
            Action invalidateCache);

        /// <summary>
        /// Invalidate a cache entry
        /// </summary>
        CacheInvalidationStrategy.CacheOperationResult InvalidateCacheEntry(
            string jobId,
            Action invalidateAction);

        /// <summary>
        /// Remove multiple cache entries
        /// </summary>
        CacheInvalidationStrategy.CacheOperationResult RemoveCacheEntries(
            IEnumerable<string> jobIds,
            Action<string> removeAction);

        /// <summary>
        /// Check cache consistency
        /// </summary>
        CacheInvalidationStrategy.CacheInconsistency? DetectCacheInconsistency(
            string jobId,
            DateTime? cachedLastUpdated,
            DateTime? dbLastUpdated,
            DateTime? cacheLoadedAt);

        /// <summary>
        /// Get a description of the cache operation result for logging
        /// </summary>
        string GetResultSummary(CacheInvalidationStrategy.CacheOperationResult result);
    }
}
