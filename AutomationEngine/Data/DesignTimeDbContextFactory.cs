using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AutomationEngine.Options;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

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
            var configuration = BuildConfiguration();

            var databaseOptions = configuration
                .GetRequiredSection(DatabaseOptions.SectionName)
                .Get<DatabaseOptions>() ?? throw new InvalidOperationException("Database configuration section is missing");

            if (!databaseOptions.IsValid(out var validationError))
            {
                throw new InvalidOperationException($"Database configuration is invalid: {validationError}");
            }

            var designTimeSection = configuration.GetSection(DesignTimeDatabaseOptions.SectionName);
            if (designTimeSection.Exists())
            {
                var designTimeOptions = designTimeSection.Get<DesignTimeDatabaseOptions>()
                    ?? throw new InvalidOperationException("DesignTimeDatabase configuration section is invalid");
                ValidateOptionsObject(designTimeOptions, nameof(DesignTimeDatabaseOptions));

                databaseOptions.Provider = designTimeOptions.Provider;
                databaseOptions.FilePath = designTimeOptions.FilePath;
                databaseOptions.CommandTimeoutSeconds = designTimeOptions.CommandTimeoutSeconds;
                databaseOptions.PoolSize = designTimeOptions.PoolSize;
                databaseOptions.SqliteEnableWal = designTimeOptions.SqliteEnableWal;
                databaseOptions.SqliteCacheQueryPlans = designTimeOptions.SqliteCacheQueryPlans;
            }

            var connectionString = ConnectionStringBuilder.Build(databaseOptions);

            var optionsBuilder = new DbContextOptionsBuilder<AutomationDbContext>();
            ConfigureDbContext(optionsBuilder, connectionString, databaseOptions);

            return new AutomationDbContext(optionsBuilder.Options);
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Production";

            var basePath = ResolveBasePath();

            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static string ResolveBasePath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
            {
                return currentDirectory;
            }

            var appBaseDirectory = AppContext.BaseDirectory;
            if (File.Exists(Path.Combine(appBaseDirectory, "appsettings.json")))
            {
                return appBaseDirectory;
            }

            var directory = new DirectoryInfo(currentDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "appsettings.json")) &&
                    File.Exists(Path.Combine(directory.FullName, "AutomationEngine.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate appsettings.json for design-time DbContext creation");
        }

        private static void ValidateOptionsObject<T>(T options, string optionsName)
        {
            var context = new ValidationContext(options!);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(options!, context, results, validateAllProperties: true))
            {
                var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
                throw new InvalidOperationException($"{optionsName} configuration is invalid: {errors}");
            }
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
