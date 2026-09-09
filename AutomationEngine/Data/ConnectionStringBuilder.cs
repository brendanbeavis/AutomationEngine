using AutomationEngine.Options;
using Microsoft.Data.Sqlite;
using System.Text;

namespace AutomationEngine.Data
{
    /// <summary>
    /// Utility for safely building and validating database connection strings.
    /// Supports SQLite and provides a path for future SQL Server migration.
    /// Centralizes timeout, pooling, and performance settings in one place.
    /// </summary>
    public static class ConnectionStringBuilder
    {
        /// <summary>
        /// Builds a database connection string from DatabaseOptions.
        /// Applies provider-specific optimizations and validates the result.
        /// </summary>
        /// <param name="options">Database configuration options.</param>
        /// <returns>A valid, formatted connection string ready for use with DbContext.</returns>
        /// <exception cref="ArgumentNullException">Thrown if options is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if options are invalid or unsupported provider.</exception>
        public static string Build(DatabaseOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!options.IsValid(out var errorMessage))
                throw new InvalidOperationException($"Database options validation failed: {errorMessage}");

            return options.Provider.ToLowerInvariant() switch
            {
                "sqlite" => BuildSqliteConnectionString(options),
                "sqlserver" => BuildSqlServerConnectionString(options),
                _ => throw new InvalidOperationException($"Unsupported database provider: {options.Provider}")
            };
        }

        /// <summary>
        /// Builds an SQLite connection string with pooling and performance settings.
        /// </summary>
        private static string BuildSqliteConnectionString(DatabaseOptions options)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = options.FilePath,
                // Pooling improves performance for repeated connections
                Pooling = true,
                // Default pool size; SQLite doesn't enforce strict limits like SQL Server
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            return builder.ConnectionString;
        }

        /// <summary>
        /// Builds a SQL Server connection string with pooling and timeout settings.
        /// For future migration support.
        /// </summary>
        private static string BuildSqlServerConnectionString(DatabaseOptions options)
        {
            // SQL Server support deferred - implement when needed
            throw new NotImplementedException("SQL Server provider not yet implemented. Use SQLite for now.");
        }

        /// <summary>
        /// Validates that the FilePath exists for SQLite or is a valid connection string for SQL Server.
        /// </summary>
        /// <param name="options">Database configuration options.</param>
        /// <returns>True if the path/connection can be used; false otherwise.</returns>
        public static bool ValidatePath(DatabaseOptions options)
        {
            if (options == null)
                return false;

            if (options.Provider.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // For SQLite, we don't validate file existence here because EF Core will create it
                // Just ensure the directory exists or can be created
                var directory = Path.GetDirectoryName(options.FilePath) ?? ".";
                try
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // For SQL Server, basic validation of connection string format
            if (options.Provider.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(options.FilePath) && options.FilePath.Contains("=");
            }

            return false;
        }

        /// <summary>
        /// Builds connection string for design-time operations (migrations, scaffolding).
        /// Uses provided options or falls back to defaults.
        /// </summary>
        public static string BuildForDesignTime(string? filePathOverride = null, string? provider = null)
        {
            var options = new DatabaseOptions
            {
                Provider = provider ?? "Sqlite",
                FilePath = filePathOverride ?? "db\\AutomationEngine.db",
                // Design-time uses conservative settings
                CommandTimeoutSeconds = 60,
                PoolSize = 5
            };

            return Build(options);
        }
    }
}
