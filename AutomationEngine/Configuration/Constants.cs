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

    }
}
