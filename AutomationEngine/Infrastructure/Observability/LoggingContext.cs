using Serilog.Context;
using System;
using System.Diagnostics;

namespace AutomationEngine.Infrastructure.Observability
{
    /// <summary>
    /// Manages correlation IDs and logging context for distributed tracing across async operations.
    /// Uses Serilog's LogContext for async-safe property storage.
    /// 
    /// Key Features:
    /// - AsyncLocal support for correlation ID flow across async boundaries
    /// - Activity scope creation for automatic context lifecycle management
    /// - Job and operation tracking for debugging
    /// - Timestamp tracking for performance diagnostics
    /// </summary>
    public class LoggingContext
    {
        private const string CorrelationIdKey = "CorrelationId";
        private const string JobIdKey = "JobId";
        private const string UserKey = "User";
        private const string OperationKey = "Operation";
        private const string RequestIdKey = "RequestId";
        private const string ActivityDurationKey = "ActivityDurationMs";

        // AsyncLocal storage for correlation ID - safer than ThreadStatic for async code
        private static readonly AsyncLocal<string?> _asyncCorrelationId = new();

        // Thread-local storage for backward compatibility
        [ThreadStatic]
        private static string? _correlationId;

        /// <summary>
        /// Gets or creates a correlation ID for the current context.
        /// Checks AsyncLocal first (for async operations), then ThreadLocal for compatibility.
        /// If one doesn't exist, generates a new GUID-based ID.
        /// </summary>
        public static string GetOrCreateCorrelationId()
        {
            var asyncId = _asyncCorrelationId.Value;
            if (!string.IsNullOrEmpty(asyncId))
            {
                return asyncId;
            }

            var threadId = _correlationId;
            if (!string.IsNullOrEmpty(threadId))
            {
                return threadId;
            }

            var newId = Guid.NewGuid().ToString("D");
            SetCorrelationId(newId);
            return newId;
        }

        /// <summary>
        /// Sets the correlation ID in the current log context.
        /// Stores in both AsyncLocal and ThreadLocal for compatibility.
        /// </summary>
        public static void SetCorrelationId(string correlationId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

            _asyncCorrelationId.Value = correlationId;
            _correlationId = correlationId;
            LogContext.PushProperty(CorrelationIdKey, correlationId);
        }

        /// <summary>
        /// Sets the Job ID in the current log context.
        /// </summary>
        public static void SetJobId(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return;

            LogContext.PushProperty(JobIdKey, jobId);
        }

        /// <summary>
        /// Sets the user/system identifier in the log context.
        /// </summary>
        public static void SetUser(string user)
        {
            LogContext.PushProperty(UserKey, user);
        }

        /// <summary>
        /// Sets the operation name in the log context.
        /// </summary>
        public static void SetOperation(string operation)
        {
            LogContext.PushProperty(OperationKey, operation);
        }

        /// <summary>
        /// Sets the HTTP request ID in the log context.
        /// </summary>
        public static void SetRequestId(string requestId)
        {
            LogContext.PushProperty(RequestIdKey, requestId);
        }

        /// <summary>
        /// Gets the current correlation ID from the log context,
        /// or returns a default if not set.
        /// </summary>
        public static string GetCurrentCorrelationId()
        {
            return _asyncCorrelationId.Value 
                ?? _correlationId 
                ?? Guid.NewGuid().ToString("D");
        }

        /// <summary>
        /// Gets the current job ID from the log context.
        /// Returns null if not set.
        /// </summary>
        public static string? GetCurrentJobId()
        {
            return null;
        }

        /// <summary>
        /// Creates an activity scope that automatically manages correlation ID lifecycle.
        /// Includes automatic duration tracking for the scope.
        /// Dispose the returned IDisposable to clean up the context and log duration.
        /// </summary>
        /// <param name="jobId">Optional job ID to track in this activity</param>
        /// <param name="operation">Optional operation name to track in this activity</param>
        /// <returns>Activity scope that should be disposed to clean up context</returns>
        public static ActivityScope CreateActivityScope(string? jobId = null, string? operation = null)
        {
            var correlationId = GetOrCreateCorrelationId();
            return new ActivityScope(correlationId, jobId, operation);
        }

        /// <summary>
        /// Activity scope that manages logging context lifecycle with automatic duration tracking.
        /// Automatically measures and logs the duration of the activity when disposed.
        /// </summary>
        public class ActivityScope : IDisposable
        {
            private readonly string _correlationId;
            private readonly string? _jobId;
            private readonly string? _operation;
            private readonly Stopwatch _stopwatch;
            private readonly List<IDisposable> _disposables;
            private bool _disposed = false;

            public ActivityScope(string correlationId, string? jobId = null, string? operation = null)
            {
                _correlationId = correlationId;
                _jobId = jobId;
                _operation = operation;
                _stopwatch = Stopwatch.StartNew();
                _disposables = new List<IDisposable>();

                // Add correlation ID
                _disposables.Add(LogContext.PushProperty(CorrelationIdKey, correlationId));

                if (jobId is not null)
                {
                    _disposables.Add(LogContext.PushProperty(JobIdKey, jobId));
                }

                if (operation is not null)
                {
                    _disposables.Add(LogContext.PushProperty(OperationKey, operation));
                }

                // Add timestamp to context
                _disposables.Add(LogContext.PushProperty("ActivityStartTime", DateTime.UtcNow));
            }

            /// <summary>
            /// Gets the correlation ID associated with this scope
            /// </summary>
            public string CorrelationId => _correlationId;

            /// <summary>
            /// Gets the duration of this activity scope
            /// </summary>
            public TimeSpan Duration => _stopwatch.Elapsed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _stopwatch.Stop();

                // Dispose in reverse order (LIFO)
                for (int i = _disposables.Count - 1; i >= 0; i--)
                {
                    _disposables[i]?.Dispose();
                }

                // Push the activity duration as a final property if needed by downstream code
                LogContext.PushProperty(ActivityDurationKey, _stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
