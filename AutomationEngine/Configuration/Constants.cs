namespace AutomationEngine.Configuration
{
    /// <summary>
    /// Centralized constants for the AutomationEngine application.
    /// Purpose: Avoid magic strings and numbers scattered throughout the codebase.
    /// All constants are organized by functional area for maintainability.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// HTTP header names and authentication constants
        /// </summary>
        public static class Headers
        {
            /// <summary>
            /// Name of the custom header used for optional admin secret validation.
            /// When AdminSecret is configured in appsettings.json, this header is required for API/SignalR requests.
            /// Allows external scripts/tools to authenticate with the API while maintaining localhost-only restriction.
            /// </summary>
            public const string AdminSecret = "X-Admin-Secret";
        }

        /// <summary>
        /// Job execution and configuration constants
        /// </summary>
        public static class Jobs
        {
            /// <summary>
            /// Default command shell to use for job execution.
            /// Windows-specific: PowerShell is the default shell for running job scripts.
            /// This value is used as a fallback when no command shell is explicitly specified.
            /// </summary>
            public const string PowerShellDefault = "powershell.exe";

            /// <summary>
            /// Maximum length allowed for job script content.
            /// Prevents extremely large scripts from consuming excessive memory or causing performance issues.
            /// Enforced both at the DTO validation level and at runtime in JobRunner.
            /// </summary>
            public const int CommandMaxLength = 5000;
        }

        /// <summary>
        /// SignalR hub path and real-time communication constants
        /// </summary>
        public static class SignalR
        {
            /// <summary>
            /// Path to the SignalR hub for job status updates.
            /// Used by both the server to map the hub and the client to establish connections.
            /// Clients connect via WebSocket/long-polling to receive real-time job completion notifications.
            /// </summary>
            public const string JobStatusHubPath = "/hubs/job-status";
        }

        /// <summary>
        /// Resilience policy thresholds (Polly circuit breaker and retry configurations)
        /// </summary>
        public static class Resilience
        {
            /// <summary>
            /// Number of handled events (exceptions or failures) allowed before the circuit breaker opens.
            /// After this threshold is reached, the circuit breaker will interrupt requests for the specified duration
            /// to allow the backend service to recover from cascading failures.
            /// </summary>
            public const int CircuitBreakerThreshold = 5;

            /// <summary>
            /// Duration (in seconds) that the circuit breaker remains open after threshold is exceeded.
            /// During this period, requests are rejected immediately without attempting to reach the backend.
            /// After the duration expires, the circuit breaker enters half-open state to test if the service has recovered.
            /// </summary>
            public const int CircuitBreakerDurationSeconds = 30;

            /// <summary>
            /// Number of retry attempts for transient failures.
            /// Used in conjunction with exponential backoff (2^attempt seconds) to handle temporary network/service issues.
            /// </summary>
            public const int RetryAttempts = 3;
        }
    }
}
