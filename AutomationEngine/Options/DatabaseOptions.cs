namespace AutomationEngine.Options
{
    /// <summary>
    /// Typed configuration for database connection settings.
    /// Centralizes connection strings, pooling, and timeout configuration.
    /// Supports environment variable override for deployment flexibility.
    /// </summary>
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        /// <summary>
        /// Gets or sets the database provider type (e.g., "Sqlite", "SqlServer").
        /// Used for connection string validation and DbContext configuration.
        /// </summary>
        public string Provider { get; set; } = "Sqlite";

        /// <summary>
        /// Gets or sets the database file path (for SQLite) or server connection string (for SQL Server).
        /// Can be overridden via AUTOMATIONENGINE_DATABASE_FILEPATH environment variable.
        /// </summary>
        public string FilePath { get; set; } = "AutomationEngine.db";

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// Applies to both connection open and command execution timeouts.
        /// Valid range: 0-600 seconds. Default: 30 seconds.
        /// </summary>
        public int CommandTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the connection pool size.
        /// For SQLite, affects how many concurrent connections are cached.
        /// For SQL Server, controls minimum pool size.
        /// Valid range: 1-1000. Default: 10 for SQLite, 100 for SQL Server.
        /// </summary>
        public int PoolSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to enable connection pool pre-warming on startup.
        /// Pre-warming creates and validates initial pool connections early.
        /// Recommended: true for production, can slow startup but catches DB issues early.
        /// </summary>
        public bool EnablePoolPrewarming { get; set; } = true;

        /// <summary>
        /// Gets or sets whether SQLite should cache query plans.
        /// Only applies to SQLite provider. Improves performance for repeated queries.
        /// </summary>
        public bool SqliteCacheQueryPlans { get; set; } = true;

        /// <summary>
        /// Gets or sets whether SQLite should use Write-Ahead Logging (WAL) mode.
        /// Only applies to SQLite provider. Improves concurrency; requires extra files on disk.
        /// </summary>
        public bool SqliteEnableWal { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate the connection on startup.
        /// A failed validation will log an error but not prevent application startup.
        /// Recommended: true for diagnosing connection issues early.
        /// </summary>
        public bool ValidateConnectionOnStartup { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for transient failures.
        /// Transient failures (e.g., locked database) trigger automatic retry with exponential backoff.
        /// Valid range: 0-10. Default: 3 retries.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Validates the configuration and returns true if all settings are valid.
        /// </summary>
        public bool IsValid(out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(Provider))
            {
                errorMessage = "Database Provider cannot be empty";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FilePath))
            {
                errorMessage = "Database FilePath cannot be empty";
                return false;
            }

            if (CommandTimeoutSeconds < 0 || CommandTimeoutSeconds > 600)
            {
                errorMessage = $"CommandTimeoutSeconds must be between 0 and 600, got {CommandTimeoutSeconds}";
                return false;
            }

            if (PoolSize < 1 || PoolSize > 1000)
            {
                errorMessage = $"PoolSize must be between 1 and 1000, got {PoolSize}";
                return false;
            }

            if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
            {
                errorMessage = $"MaxRetryAttempts must be between 0 and 10, got {MaxRetryAttempts}";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
