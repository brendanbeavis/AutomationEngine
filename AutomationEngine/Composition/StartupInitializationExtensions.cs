using AutomationEngine.Application.Abstractions;
using AutomationEngine.Data;
using AutomationEngine.Infrastructure.Persistence;
using AutomationEngine.Options;
using AutomationEngine.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace AutomationEngine.Composition;

public static class StartupInitializationExtensions
{
    public static async Task InitializeAutomationEngineAsync(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var validator = scope.ServiceProvider.GetRequiredService<DatabaseConnectionValidator>();
            var runtimeDatabaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            if (runtimeDatabaseOptions.ValidateConnectionOnStartup)
            {
                Log.Information("Database validation: Starting connection test");
                await validator.ValidateAndLogAsync();
                Log.Information("Database validation: Connection validated successfully");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database validation encountered an error but will continue");
        }

        try
        {
            using var scope = app.Services.CreateScope();
            Log.Information("Database migrations: Starting migration process");
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await db.Database.MigrateAsync();
            sw.Stop();
            Log.Information("Database migrations: Applied successfully in {DurationMs}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to apply database migrations - application cannot continue");
            throw;
        }

        try
        {
            using var scope = app.Services.CreateScope();
            Log.Information("JobStateManager: Starting initialization and loading jobs");
            var stateManager = scope.ServiceProvider.GetRequiredService<IJobStateManager>();
            var stateTransition = scope.ServiceProvider.GetRequiredService<IJobStateTransitionService>();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await stateManager.LoadJobsAsync();
            sw.Stop();
            Log.Information("JobStateManager: Loaded successfully in {DurationMs}ms", sw.ElapsedMilliseconds);

            sw.Restart();
            await stateTransition.ValidateAndCleanupStaleRunningStatesAsync();
            sw.Stop();
            Log.Information("Startup: Validated running job states in {DurationMs}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to initialize JobStateManager - application cannot continue");
            throw;
        }
    }
}

