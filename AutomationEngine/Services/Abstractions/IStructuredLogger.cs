using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services.Abstractions
{
    /// <summary>
    /// Interface for structured logging utility
    /// Provides semantic logging methods with automatic context enrichment and correlation IDs
    /// </summary>
    public interface IStructuredLogger
    {
        // Note: Since this is typically used as a static utility class,
        // this interface serves as a contract for structured logging operations
        // The actual implementation uses extension methods on ILogger<T>
    }

    /// <summary>
    /// Extension methods for structured logging
    /// </summary>
    public static class StructuredLoggerExtensions
    {
        /// <summary>
        /// Log an exception with classification
        /// </summary>
        public static void LogExceptionWithClassification<T>(this ILogger<T> logger, 
            Exception exception, string context) where T : class
        {
            var classification = ErrorClassifier.Classify(exception);
            logger.LogError(exception, 
                "Exception occurred | Context: {Context} | Classification: {Classification}",
                context, classification);
        }

        /// <summary>
        /// Log an operation with timing information
        /// </summary>
        public static void LogWithTiming<T>(this ILogger<T> logger, 
            string operation, TimeSpan duration, string? details = null) where T : class
        {
            logger.LogInformation(
                "Operation completed | Operation: {Operation} | Duration: {DurationMs}ms | Details: {Details}",
                operation, duration.TotalMilliseconds, details ?? "");
        }

        /// <summary>
        /// Log with correlation ID for tracing
        /// </summary>
        public static void LogWithCorrelation<T>(this ILogger<T> logger, 
            string message, string correlationId) where T : class
        {
            logger.LogInformation(
                "Message: {Message} | CorrelationId: {CorrelationId}",
                message, correlationId);
        }
    }
}
