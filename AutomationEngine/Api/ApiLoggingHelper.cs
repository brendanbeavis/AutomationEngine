using AutomationEngine.Infrastructure.Observability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AutomationEngine.Api
{
    /// <summary>
    /// Helper class for logging API operations with consistent metrics and tracing.
    /// Provides methods to log API requests, responses, and errors with timing information.
    /// </summary>
    public static class ApiLoggingHelper
    {
        /// <summary>
        /// Logs an API GET request.
        /// </summary>
        public static void LogGetRequest<T>(ILogger<T> logger, string endpoint, string? jobId = null) where T : class
        {
            if (!string.IsNullOrEmpty(jobId))
            {
                logger.LogInformation("API GET request | Endpoint: {Endpoint} | JobId: {JobId}", endpoint, jobId);
            }
            else
            {
                logger.LogInformation("API GET request | Endpoint: {Endpoint}", endpoint);
            }
        }

        /// <summary>
        /// Logs an API POST request with timing.
        /// </summary>
        public static void LogPostRequest<T>(ILogger<T> logger, string endpoint, string? jobId, TimeSpan duration, int statusCode) where T : class
        {
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            var message = string.IsNullOrEmpty(jobId)
                ? "API POST request | Endpoint: {Endpoint} | StatusCode: {StatusCode} | Duration: {DurationMs}ms"
                : "API POST request | Endpoint: {Endpoint} | JobId: {JobId} | StatusCode: {StatusCode} | Duration: {DurationMs}ms";

            if (string.IsNullOrEmpty(jobId))
            {
                logger.Log(logLevel, message, endpoint, statusCode, duration.TotalMilliseconds);
            }
            else
            {
                logger.Log(logLevel, message, endpoint, jobId, statusCode, duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Logs an API PUT/PATCH request with timing.
        /// </summary>
        public static void LogUpdateRequest<T>(ILogger<T> logger, string endpoint, string? jobId, string method, TimeSpan duration, int statusCode) where T : class
        {
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            logger.Log(logLevel,
                "API {Method} request | Endpoint: {Endpoint} | JobId: {JobId} | StatusCode: {StatusCode} | Duration: {DurationMs}ms",
                method, endpoint, jobId ?? "", statusCode, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs an API DELETE request with timing.
        /// </summary>
        public static void LogDeleteRequest<T>(ILogger<T> logger, string endpoint, string jobId, TimeSpan duration, int statusCode) where T : class
        {
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            logger.Log(logLevel,
                "API DELETE request | Endpoint: {Endpoint} | JobId: {JobId} | StatusCode: {StatusCode} | Duration: {DurationMs}ms",
                endpoint, jobId, statusCode, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs a validation error in API request.
        /// </summary>
        public static void LogValidationError<T>(ILogger<T> logger, string endpoint, string? jobId, string errorMessage) where T : class
        {
            logger.LogWarning("API validation error | Endpoint: {Endpoint} | JobId: {JobId} | Error: {Error}",
                endpoint, jobId ?? "", errorMessage);
        }

        /// <summary>
        /// Logs an API error.
        /// </summary>
        public static void LogApiError<T>(ILogger<T> logger, string endpoint, string? jobId, Exception ex) where T : class
        {
            var classification = ErrorClassifier.Classify(ex);
            logger.LogError(ex,
                "API error | Endpoint: {Endpoint} | JobId: {JobId} | Classification: {Classification}",
                endpoint, jobId ?? "", classification);
        }

        /// <summary>
        /// Logs a successful operation with job details.
        /// </summary>
        public static void LogOperationSuccess<T>(ILogger<T> logger, string operation, string jobId, string details) where T : class
        {
            logger.LogInformation("API operation successful | Operation: {Operation} | JobId: {JobId} | Details: {Details}",
                operation, jobId, details);
        }
    }
}
