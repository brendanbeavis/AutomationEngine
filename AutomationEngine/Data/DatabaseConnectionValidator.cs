using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AutomationEngine.Data
{
    /// <summary>
    /// Service for validating database connectivity on startup.
    /// Attempts to open a test connection and checks for common issues.
    /// Errors are logged but don't prevent startup (graceful degradation).
    /// </summary>
    public class DatabaseConnectionValidator
    {
        private readonly IDbContextFactory<AutomationDbContext> _contextFactory;
        private readonly ILogger<DatabaseConnectionValidator> _logger;

        public DatabaseConnectionValidator(
            IDbContextFactory<AutomationDbContext> contextFactory,
            ILogger<DatabaseConnectionValidator> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates database connectivity by attempting to open a connection
        /// and execute a simple query. Returns true if successful.
        /// </summary>
        public async Task<bool> ValidateConnectionAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                using var connection = context.Database.GetDbConnection();

                // Attempt to open the connection
                _logger.LogInformation("Validating database connection...");
                await connection.OpenAsync();

                // Execute a simple query to ensure the database is accessible
                var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandTimeout = 10;

                var result = await command.ExecuteScalarAsync();

                _logger.LogInformation("Database connection validation successful");
                return true;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 14) // SQLITE_CANTOPEN
            {
                _logger.LogError(ex, "Database file cannot be opened. Check file permissions and path.");
                return false;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1) // SQLITE_ERROR
            {
                _logger.LogError(ex, "SQLite error during connection validation. Database may be corrupted.");
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error during validation");
                return false;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Database connection validation timed out");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during database connection validation");
                return false;
            }
        }

        /// <summary>
        /// Validates database connectivity and logs results.
        /// Non-fatal; application can continue even if validation fails.
        /// </summary>
        public async Task ValidateAndLogAsync()
        {
            var isValid = await ValidateConnectionAsync();

            if (!isValid)
            {
                _logger.LogWarning(
                    "Database connection validation failed. Application will continue but database operations may fail. " +
                    "Check database file permissions, path configuration, and disk space.");
            }
        }
    }
}
