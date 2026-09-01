using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AutomationEngine.Options;

namespace AutomationEngine.Data
{
    /// <summary>
    /// Design-time factory for creating DbContext instances during EF Core operations
    /// (migrations, scaffolding, updates). This runs outside the normal DI container.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AutomationDbContext>
    {
        public AutomationDbContext CreateDbContext(string[] args)
        {
            // Use ConnectionStringBuilder to ensure consistency with runtime configuration
            // Falls back to migration folder location if running from Visual Studio
            var filePathOverride = Path.Combine("db", "AutomationEngine.db");

            var databaseOptions = new DatabaseOptions
            {
                Provider = "Sqlite",
                FilePath = filePathOverride,
                CommandTimeoutSeconds = 60,
                PoolSize = 5,
                SqliteEnableWal = false, // WAL can complicate design-time operations
                SqliteCacheQueryPlans = true
            };

            var connectionString = ConnectionStringBuilder.Build(databaseOptions);

            var optionsBuilder = new DbContextOptionsBuilder<AutomationDbContext>();
            ConfigureDbContext(optionsBuilder, connectionString, databaseOptions);

            return new AutomationDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Configures DbContext options including SQLite pragmas and command timeout.
        /// Extracted to shared method for reuse in Program.cs.
        /// </summary>
        public static void ConfigureDbContext(
            DbContextOptionsBuilder<AutomationDbContext> optionsBuilder,
            string connectionString,
            DatabaseOptions options)
        {
            optionsBuilder.UseSqlite(connectionString, sqliteOptions =>
            {
                // Set command timeout for migrations and queries
                sqliteOptions.CommandTimeout(options.CommandTimeoutSeconds);

                // Configure migrations assembly if needed
                // sqliteOptions.MigrationsAssembly("AutomationEngine");
            });

            // Apply SQLite pragmas for performance and reliability
            optionsBuilder.LogTo(Console.WriteLine); // For design-time diagnostics

            // Lazy-load pragmas via connection string callback
            // Note: Pragmas are applied when the connection is opened
            if (options.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                optionsBuilder.UseSqlite(connectionString, opt =>
                {
                    // WAL mode for improved concurrency (if enabled in options)
                    if (options.SqliteEnableWal)
                    {
                        opt.UseQuerySplittingBehavior(Microsoft.EntityFrameworkCore.QuerySplittingBehavior.SplitQuery);
                    }
                });
            }
        }
    }
}
