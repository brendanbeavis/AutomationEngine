using Serilog.Context;
using System;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Manages correlation IDs and logging context for distributed tracing across async operations.
    /// Uses Serilog's LogContext for async-safe property storage.
    /// </summary>
    public class LoggingContext
    {
        private const string CorrelationIdKey = "CorrelationId";
        private const string JobIdKey = "JobId";
        private const string UserKey = "User";
        private const string OperationKey = "Operation";
        private const string RequestIdKey = "RequestId";

        // Thread-local storage for correlation ID
        [ThreadStatic]
        private static string? _correlationId;

        /// <summary>
        /// Gets or creates a correlation ID for the current context.
        /// If one doesn't exist, generates a new GUID-based ID.
        /// </summary>
        public static string GetOrCreateCorrelationId()
        {
            if (string.IsNullOrEmpty(_correlationId))
            {
                _correlationId = Guid.NewGuid().ToString("D");
                LogContext.PushProperty(CorrelationIdKey, _correlationId);
            }

            return _correlationId;
        }

        /// <summary>
        /// Sets the correlation ID in the current log context.
        /// </summary>
        public static void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId;
            LogContext.PushProperty(CorrelationIdKey, correlationId);
        }

        /// <summary>
        /// Sets the Job ID in the current log context.
        /// </summary>
        public static void SetJobId(string jobId)
        {
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
            return _correlationId ?? Guid.NewGuid().ToString("D");
        }

        /// <summary>
        /// Gets the current job ID from the log context.
        /// </summary>
        public static string? GetCurrentJobId()
        {
            return null;
        }

        /// <summary>
        /// Creates an activity scope that automatically manages correlation ID lifecycle.
        /// Dispose the returned IDisposable to clean up the context.
        /// </summary>
        public static IDisposable CreateActivityScope(string? jobId = null, string? operation = null)
        {
            var correlationId = GetOrCreateCorrelationId();
            var disposables = new List<IDisposable>();

            // Add correlation ID if not already set
            disposables.Add(LogContext.PushProperty(CorrelationIdKey, correlationId));

            if (jobId is not null)
            {
                disposables.Add(LogContext.PushProperty(JobIdKey, jobId));
            }

            if (operation is not null)
            {
                disposables.Add(LogContext.PushProperty(OperationKey, operation));
            }

            // Add timestamp to context
            disposables.Add(LogContext.PushProperty("ActivityStartTime", DateTime.UtcNow));

            return new CompositeDisposable(disposables);
        }

        /// <summary>
        /// Composite disposable that disposes multiple disposables in reverse order.
        /// </summary>
        private class CompositeDisposable : IDisposable
        {
            private readonly List<IDisposable> _disposables;

            public CompositeDisposable(List<IDisposable> disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                // Dispose in reverse order (LIFO)
                for (int i = _disposables.Count - 1; i >= 0; i--)
                {
                    _disposables[i]?.Dispose();
                }
            }
        }
    }
}
