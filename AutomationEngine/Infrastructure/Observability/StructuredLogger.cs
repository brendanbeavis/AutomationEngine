using Microsoft.Extensions.Logging;
using System;

namespace AutomationEngine.Infrastructure.Observability
{
    /// <summary>
    /// Structured logging utility class providing semantic logging methods
    /// with automatic context enrichment and correlation IDs.
    /// </summary>
    public static class StructuredLogger
    {
        /// <summary>
        /// Logs a job start event with full context.
        /// </summary>
        public static void LogJobStarted<T>(ILogger<T> logger, string jobId, string displayName, string jobType)
            where T : class
        {
            logger.LogInformation(
                "Job execution started | JobId: {JobId} | DisplayName: {DisplayName} | Type: {JobType}",
                jobId, displayName, jobType);
        }

        /// <summary>
        /// Logs a job completion event with success status and duration.
        /// </summary>
        public static void LogJobCompleted<T>(ILogger<T> logger, string jobId, string displayName,
            TimeSpan duration, bool success, string? output = null) where T : class
        {
            if (success)
            {
                logger.LogInformation(
                    "Job execution completed | JobId: {JobId} | DisplayName: {DisplayName} | Duration: {DurationMs}ms",
                    jobId, displayName, duration.TotalMilliseconds);
            }
            else
            {
                logger.LogWarning(
                    "Job execution completed with failure | JobId: {JobId} | DisplayName: {DisplayName} | Duration: {DurationMs}ms | Output: {Output}",
                    jobId, displayName, duration.TotalMilliseconds, output ?? "");
            }
        }

        /// <summary>
        /// Logs a job failure event with exception details.
        /// </summary>
        public static void LogJobFailed<T>(ILogger<T> logger, string jobId, string displayName,
            Exception exception, int? exitCode = null) where T : class
        {
            var classification = ErrorClassifier.Classify(exception);
            logger.LogError(exception,
                "Job execution failed | JobId: {JobId} | DisplayName: {DisplayName} | Classification: {ErrorClassification} | ExitCode: {ExitCode}",
                jobId, displayName, classification, exitCode ?? -1);
        }

        /// <summary>
        /// Logs a process execution event.
        /// </summary>
        public static void LogProcessExecution<T>(ILogger<T> logger, string jobId, string fileName,
            string arguments, int exitCode) where T : class
        {
            if (exitCode == 0)
            {
                logger.LogInformation(
                    "Process execution succeeded | JobId: {JobId} | Command: {FileName} | Arguments: {Arguments}",
                    jobId, fileName, arguments);
            }
            else
            {
                logger.LogWarning(
                    "Process execution failed | JobId: {JobId} | Command: {FileName} | Arguments: {Arguments} | ExitCode: {ExitCode}",
                    jobId, fileName, arguments, exitCode);
            }
        }

        /// <summary>
        /// Logs a PowerShell script execution event.
        /// </summary>
        public static void LogPowerShellExecution<T>(ILogger<T> logger, string jobId, string script,
            int exitCode) where T : class
        {
            if (exitCode == 0)
            {
                logger.LogInformation(
                    "PowerShell execution succeeded | JobId: {JobId} | Script: {Script}",
                    jobId, script);
            }
            else
            {
                logger.LogWarning(
                    "PowerShell execution failed | JobId: {JobId} | Script: {Script} | ExitCode: {ExitCode}",
                    jobId, script, exitCode);
            }
        }

        /// <summary>
        /// Logs a file cleanup operation.
        /// </summary>
        public static void LogFileCleanupOperation<T>(ILogger<T> logger, string jobId, string targetFolder,
            int filesDeleted, string filter, bool recurse, TimeSpan duration) where T : class
        {
            logger.LogInformation(
                "File cleanup operation completed | JobId: {JobId} | TargetFolder: {TargetFolder} | FilesDeleted: {FilesDeleted} | Filter: {Filter} | Recurse: {Recurse} | Duration: {DurationMs}ms",
                jobId, targetFolder, filesDeleted, filter, recurse, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs a job state transition.
        /// </summary>
        public static void LogJobStateTransition<T>(ILogger<T> logger, string jobId, string fromState,
            string toState, string? reason = null) where T : class
        {
            logger.LogInformation(
                "Job state transition | JobId: {JobId} | From: {FromState} | To: {ToState} | Reason: {Reason}",
                jobId, fromState, toState, reason ?? "");
        }

        /// <summary>
        /// Logs a database operation.
        /// </summary>
        public static void LogDatabaseOperation<T>(ILogger<T> logger, string operation, string entityType,
            int affectedRows, TimeSpan duration) where T : class
        {
            logger.LogInformation(
                "Database operation completed | Operation: {Operation} | EntityType: {EntityType} | AffectedRows: {AffectedRows} | Duration: {DurationMs}ms",
                operation, entityType, affectedRows, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs an API call.
        /// </summary>
        public static void LogApiCall<T>(ILogger<T> logger, string method, string path, int statusCode,
            TimeSpan duration) where T : class
        {
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            if (logLevel == LogLevel.Information)
            {
                logger.LogInformation(
                    "API call completed | Method: {Method} | Path: {Path} | StatusCode: {StatusCode} | Duration: {DurationMs}ms",
                    method, path, statusCode, duration.TotalMilliseconds);
            }
            else if (logLevel == LogLevel.Warning)
            {
                logger.LogWarning(
                    "API call returned warning status | Method: {Method} | Path: {Path} | StatusCode: {StatusCode} | Duration: {DurationMs}ms",
                    method, path, statusCode, duration.TotalMilliseconds);
            }
            else
            {
                logger.LogError(
                    "API call failed | Method: {Method} | Path: {Path} | StatusCode: {StatusCode} | Duration: {DurationMs}ms",
                    method, path, statusCode, duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Logs a validation error with details.
        /// </summary>
        public static void LogValidationError<T>(ILogger<T> logger, string entity, string property,
            string errorMessage) where T : class
        {
            logger.LogWarning(
                "Validation error | Entity: {Entity} | Property: {Property} | Error: {ErrorMessage}",
                entity, property, errorMessage);
        }

        /// <summary>
        /// Logs a retry attempt
        /// </summary>
        public static void LogRetryAttempt<T>(ILogger<T> logger, string jobId, int attemptNumber, 
            int maxAttempts, TimeSpan backoffDuration, Exception lastException)
        {
            logger.LogWarning(lastException,
                "Retry attempt | JobId: {JobId} | Attempt: {CurrentAttempt}/{MaxAttempts} | BackoffDuration: {BackoffMs}ms",
                jobId, attemptNumber, maxAttempts, backoffDuration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs a configuration issue.
        /// </summary>
        public static void LogConfigurationIssue<T>(ILogger<T> logger, string configKey, string message)
        {
            logger.LogWarning(
                "Configuration issue | ConfigKey: {ConfigKey} | Message: {Message}",
                configKey, message);
        }

        /// <summary>
        /// Logs application startup.
        /// </summary>
        public static void LogApplicationStartup<T>(ILogger<T> logger, string version, string environment)
        {
            logger.LogInformation(
                "Application started | Version: {Version} | Environment: {Environment}",
                version, environment);
        }

        /// <summary>
        /// Logs application shutdown.
        /// </summary>
        public static void LogApplicationShutdown<T>(ILogger<T> logger, TimeSpan uptime, string? reason = null)
        {
            logger.LogInformation(
                "Application shutting down | Uptime: {UptimeMs}ms | Reason: {Reason}",
                uptime.TotalMilliseconds, reason ?? "normal");
        }

        /// <summary>
        /// Logs a scheduler event.
        /// </summary>
        public static void LogSchedulerEvent<T>(ILogger<T> logger, string eventType, string jobId, 
            string eventDescription)
        {
            logger.LogInformation(
                "Scheduler event | EventType: {EventType} | JobId: {JobId} | Description: {Description}",
                eventType, jobId, eventDescription);
        }

        /// <summary>
        /// Logs a performance warning when an operation takes too long.
        /// </summary>
        public static void LogPerformanceWarning<T>(ILogger<T> logger, string operation, TimeSpan duration, 
            TimeSpan? warningThreshold = null)
        {
            var threshold = warningThreshold?.TotalMilliseconds ?? 1000;
            logger.LogWarning(
                "Performance warning | Operation: {Operation} | Duration: {DurationMs}ms | Threshold: {ThresholdMs}ms",
                operation, duration.TotalMilliseconds, threshold);
        }

        /// <summary>
        /// Logs a cache operation result (update, invalidate, refresh).
        /// </summary>
        public static void LogCacheOperation<T>(ILogger<T> logger, string jobId, string operation, 
            bool success, string? errorMessage = null, TimeSpan? duration = null) where T : class
        {
            if (success)
            {
                if (duration.HasValue)
                {
                    logger.LogDebug(
                        "Cache operation completed | JobId: {JobId} | Operation: {Operation} | Duration: {DurationMs}ms",
                        jobId, operation, duration.Value.TotalMilliseconds);
                }
                else
                {
                    logger.LogDebug(
                        "Cache operation completed | JobId: {JobId} | Operation: {Operation}",
                        jobId, operation);
                }
            }
            else
            {
                logger.LogWarning(
                    "Cache operation failed | JobId: {JobId} | Operation: {Operation} | Error: {ErrorMessage}",
                    jobId, operation, errorMessage ?? "unknown");
            }
        }

        /// <summary>
        /// Logs a job state transition with optional metrics.
        /// </summary>
        public static void LogJobSaveOperation<T>(ILogger<T> logger, string jobId, string displayName, 
            bool isCreate, TimeSpan duration) where T : class
        {
            var operation = isCreate ? "JobCreated" : "JobUpdated";
            logger.LogInformation(
                "Job saved successfully | JobId: {JobId} | DisplayName: {DisplayName} | Operation: {Operation} | Duration: {DurationMs}ms",
                jobId, displayName, operation, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs a job refresh operation from database.
        /// </summary>
        public static void LogJobRefresh<T>(ILogger<T> logger, string jobId, bool found, TimeSpan duration) where T : class
        {
            if (found)
            {
                logger.LogDebug(
                    "Job refreshed from database | JobId: {JobId} | Duration: {DurationMs}ms",
                    jobId, duration.TotalMilliseconds);
            }
            else
            {
                logger.LogInformation(
                    "Job not found in database, cache invalidated | JobId: {JobId}",
                    jobId);
            }
        }

        /// <summary>
        /// Logs a job deletion operation.
        /// </summary>
        public static void LogJobDeletion<T>(ILogger<T> logger, string jobId, bool success, 
            int? relatedRunsCount = null, TimeSpan? duration = null) where T : class
        {
            if (success)
            {
                if (relatedRunsCount.HasValue && duration.HasValue)
                {
                    logger.LogInformation(
                        "Job soft-deleted successfully | JobId: {JobId} | RelatedRuns: {RunsCount} | Duration: {DurationMs}ms",
                        jobId, relatedRunsCount, duration.Value.TotalMilliseconds);
                }
                else
                {
                    logger.LogInformation(
                        "Job soft-deleted successfully | JobId: {JobId}",
                        jobId);
                }
            }
            else
            {
                logger.LogError(
                    "Job soft-deletion failed | JobId: {JobId}",
                    jobId);
            }
        }

        /// <summary>
        /// Logs a background task operation.
        /// </summary>
        public static void LogBackgroundTask<T>(ILogger<T> logger, string jobId, string operation, 
            bool success, string? details = null) where T : class
        {
            if (success)
            {
                logger.LogDebug(
                    "Background task completed | JobId: {JobId} | Operation: {Operation}",
                    jobId, operation);
            }
            else
            {
                logger.LogWarning(
                    "Background task failed | JobId: {JobId} | Operation: {Operation} | Details: {Details}",
                    jobId, operation, details ?? "");
            }
        }

        /// <summary>
        /// Logs an audit logging operation.
        /// </summary>
        public static void LogAuditOperation<T>(ILogger<T> logger, string jobId, string action, 
            bool success, string? details = null) where T : class
        {
            if (success)
            {
                logger.LogDebug(
                    "Audit event logged | JobId: {JobId} | Action: {Action}",
                    jobId, action);
            }
            else
            {
                logger.LogError(
                    "Audit logging failed | JobId: {JobId} | Action: {Action} | Details: {Details}",
                    jobId, action, details ?? "");
            }
        }

        /// <summary>
        /// Logs security-related operations (authentication, authorization).
        /// </summary>
        public static void LogSecurityEvent<T>(ILogger<T> logger, string eventType, string source, 
            bool success, string? details = null) where T : class
        {
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            var statusText = success ? "authenticated" : "rejected";

            if (logLevel == LogLevel.Information)
            {
                logger.LogInformation(
                    "Security event {statusText} | EventType: {EventType} | Source: {Source} | Details: {Details}",
                    statusText, eventType, source, details ?? "");
            }
            else
            {
                logger.LogWarning(
                    "Security event {statusText} | EventType: {EventType} | Source: {Source} | Details: {Details}",
                    statusText, eventType, source, details ?? "");
            }
        }

        /// <summary>
        /// Logs a SignalR hub operation (connect, disconnect, subscribe, unsubscribe).
        /// </summary>
        public static void LogSignalROperation<T>(ILogger<T> logger, string operation, string connectionId, 
            string? jobId = null) where T : class
        {
            if (!string.IsNullOrEmpty(jobId))
            {
                logger.LogDebug(
                    "SignalR operation | Operation: {Operation} | ConnectionId: {ConnectionId} | JobId: {JobId}",
                    operation, connectionId, jobId);
            }
            else
            {
                logger.LogDebug(
                    "SignalR operation | Operation: {Operation} | ConnectionId: {ConnectionId}",
                    operation, connectionId);
            }
        }

        /// <summary>
        /// Logs service initialization.
        /// </summary>
        public static void LogServiceInitialization<T>(ILogger<T> logger, string serviceName, 
            bool success, string? details = null) where T : class
        {
            if (success)
            {
                logger.LogInformation(
                    "Service initialization successful | Service: {ServiceName} | Details: {Details}",
                    serviceName, details ?? "");
            }
            else
            {
                logger.LogError(
                    "Service initialization failed | Service: {ServiceName} | Details: {Details}",
                    serviceName, details ?? "");
            }
        }

        /// <summary>
        /// Logs cache operation with metrics.
        /// </summary>
        public static void LogCacheOperation<T>(ILogger<T> logger, string operation, string cacheKey,
            bool success, TimeSpan? duration = null, string? details = null) where T : class
        {
            if (success)
            {
                var message = duration.HasValue
                    ? "Cache operation | Operation: {Operation} | Key: {CacheKey} | Duration: {DurationMs}ms"
                    : "Cache operation | Operation: {Operation} | Key: {CacheKey}";

                if (duration.HasValue)
                {
                    logger.LogDebug(message, operation, cacheKey, duration.Value.TotalMilliseconds);
                }
                else
                {
                    logger.LogDebug(message, operation, cacheKey);
                }
            }
            else
            {
                logger.LogWarning(
                    "Cache operation failed | Operation: {Operation} | Key: {CacheKey} | Details: {Details}",
                    operation, cacheKey, details ?? "unknown error");
            }
        }

        /// <summary>
        /// Logs notification sending operation.
        /// </summary>
        public static void LogNotificationOperation<T>(ILogger<T> logger, string notificationType, 
            string? jobId, bool success, TimeSpan? duration = null, string? details = null) where T : class
        {
            if (success)
            {
                if (duration.HasValue)
                {
                    logger.LogInformation(
                        "Notification sent successfully | Type: {NotificationType} | JobId: {JobId} | Duration: {DurationMs}ms",
                        notificationType, jobId ?? "", duration.Value.TotalMilliseconds);
                }
                else
                {
                    logger.LogInformation(
                        "Notification sent successfully | Type: {NotificationType} | JobId: {JobId}",
                        notificationType, jobId ?? "");
                }
            }
            else
            {
                logger.LogError(
                    "Notification send failed | Type: {NotificationType} | JobId: {JobId} | Details: {Details}",
                    notificationType, jobId ?? "", details ?? "unknown error");
            }
        }

        /// <summary>
        /// Logs background task queue operation.
        /// </summary>
        public static void LogBackgroundTaskQueue<T>(ILogger<T> logger, string operation, 
            int queueLength, TimeSpan? duration = null) where T : class
        {
            logger.LogDebug(
                "Background task queue | Operation: {Operation} | QueueLength: {QueueLength} | Duration: {DurationMs}ms",
                operation, queueLength, duration?.TotalMilliseconds ?? 0);
        }

        /// <summary>
        /// Logs middleware operation.
        /// </summary>
        public static void LogMiddlewareOperation<T>(ILogger<T> logger, string middlewareName, 
            bool success, string? details = null) where T : class
        {
            if (success)
            {
                logger.LogDebug(
                    "Middleware operation successful | Middleware: {MiddlewareName} | Details: {Details}",
                    middlewareName, details ?? "");
            }
            else
            {
                logger.LogWarning(
                    "Middleware operation failed | Middleware: {MiddlewareName} | Details: {Details}",
                    middlewareName, details ?? "");
            }
        }

        /// <summary>
        /// Logs dependency injection container registration.
        /// </summary>
        public static void LogDependencyRegistration<T>(ILogger<T> logger, string serviceName, 
            string implementationName, string lifetime) where T : class
        {
            logger.LogDebug(
                "Dependency registered | Service: {ServiceName} | Implementation: {ImplementationName} | Lifetime: {Lifetime}",
                serviceName, implementationName, lifetime);
        }

        /// <summary>
        /// Logs resource acquisition/release operations.
        /// </summary>
        public static void LogResourceOperation<T>(ILogger<T> logger, string resource, string operation,
            bool success, TimeSpan? duration = null) where T : class
        {
            if (success)
            {
                if (duration.HasValue)
                {
                    logger.LogDebug(
                        "Resource operation | Resource: {Resource} | Operation: {Operation} | Duration: {DurationMs}ms",
                        resource, operation, duration.Value.TotalMilliseconds);
                }
                else
                {
                    logger.LogDebug(
                        "Resource operation | Resource: {Resource} | Operation: {Operation}",
                        resource, operation);
                }
            }
            else
            {
                logger.LogWarning(
                    "Resource operation failed | Resource: {Resource} | Operation: {Operation}",
                    resource, operation);
            }
        }

        /// <summary>
        /// Logs application lifecycle events (startup, shutdown, graceful shutdown).
        /// </summary>
        public static void LogApplicationLifecycle<T>(ILogger<T> logger, string eventType, 
            string? details = null, TimeSpan? duration = null) where T : class
        {
            var message = duration.HasValue
                ? "Application lifecycle event | EventType: {EventType} | Details: {Details} | Duration: {DurationMs}ms"
                : "Application lifecycle event | EventType: {EventType} | Details: {Details}";

            if (duration.HasValue)
            {
                logger.LogInformation(message, eventType, details ?? "", duration.Value.TotalMilliseconds);
            }
            else
            {
                logger.LogInformation(message, eventType, details ?? "");
            }
        }

        /// <summary>
        /// Logs performance metrics and thresholds exceeded.
        /// </summary>
        public static void LogPerformanceMetric<T>(ILogger<T> logger, string metricName, 
            double value, string unit, double? thresholdValue = null) where T : class
        {
            if (thresholdValue.HasValue && value > thresholdValue.Value)
            {
                logger.LogWarning(
                    "Performance metric exceeded threshold | Metric: {MetricName} | Value: {Value} | Unit: {Unit} | Threshold: {ThresholdValue}",
                    metricName, value, unit, thresholdValue.Value);
            }
            else
            {
                logger.LogDebug(
                    "Performance metric | Metric: {MetricName} | Value: {Value} | Unit: {Unit}",
                    metricName, value, unit);
            }
        }
    }

    /// <summary>
    /// Extension methods for ILogger to support semantic logging.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs an exception with classification and context.
        /// </summary>
        public static void LogExceptionWithClassification<T>(this ILogger<T> logger, 
            string jobId, Exception exception)
        {
            var classification = ErrorClassifier.Classify(exception);
            logger.LogError(exception,
                "Exception with classification | JobId: {JobId} | Classification: {Classification}",
                jobId, classification);
        }

        /// <summary>
        /// Logs an operation with timing information.
        /// </summary>
        public static void LogWithTiming<T>(this ILogger<T> logger, 
            string operation, TimeSpan duration)
        {
            logger.LogInformation(
                "Operation completed with timing | Operation: {Operation} | Duration: {DurationMs}ms",
                operation, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs with correlation ID context.
        /// </summary>
        public static void LogWithCorrelation<T>(this ILogger<T> logger, 
            string jobId, string message)
        {
            var correlationId = LoggingContext.GetCurrentCorrelationId();
            logger.LogInformation(
                "Message with correlation | JobId: {JobId} | CorrelationId: {CorrelationId} | Message: {Message}",
                jobId, correlationId, message);
        }
    }
}
