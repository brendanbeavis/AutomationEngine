using AutomationEngine.Application.Abstractions;
using AutomationEngine.Application.UseCases.Jobs;
using AutomationEngine.Configuration;
using AutomationEngine.Data;
using AutomationEngine.Infrastructure.Persistence;
using AutomationEngine.Infrastructure.Persistence.Repositories;
using AutomationEngine.Options;
using AutomationEngine.Options.Interfaces;
using AutomationEngine.Services;
using AutomationEngine.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Serilog;
using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Composition;

public sealed record StartupConfiguration(
    DatabaseOptions Database,
    ServerOptions Server,
    ResilienceOptions Resilience,
    string ConnectionString);

public static class ServiceRegistrationExtensions
{
    public static StartupConfiguration ConfigureAutomationEngineServices(this WebApplicationBuilder builder)
    {
        Log.Debug("Configuring typed options from appsettings with startup validation");
        builder.Services.AddAutomationEngineOptions(builder.Configuration);

        var databaseOptions = builder.Configuration.GetRequiredSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? throw new InvalidOperationException("Database configuration section is missing");

        if (!databaseOptions.IsValid(out var dbValidationError))
        {
            Log.Fatal("Database configuration is invalid: {Error}", dbValidationError);
            throw new InvalidOperationException($"Database configuration is invalid: {dbValidationError}");
        }

        var serverConfig = builder.Configuration.GetRequiredSection(ServerOptions.SectionName)
            .Get<ServerOptions>() ?? throw new InvalidOperationException("Server configuration section is missing");
        ValidateOptionsObject(serverConfig, nameof(ServerOptions));

        var resilienceOptions = builder.Configuration.GetRequiredSection(ResilienceOptions.SectionName)
            .Get<ResilienceOptions>() ?? throw new InvalidOperationException("Resilience configuration section is missing");
        ValidateOptionsObject(resilienceOptions, nameof(ResilienceOptions));

        var connectionString = ConnectionStringBuilder.Build(databaseOptions);
        Log.Information("Database configured: Provider={Provider}, Timeout={TimeoutSec}s, Pool={PoolSize}, WAL={Wal}",
            databaseOptions.Provider, databaseOptions.CommandTimeoutSeconds, databaseOptions.PoolSize, databaseOptions.SqliteEnableWal);

        builder.Services.AddDbContextFactory<AutomationDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
            });
        });

        builder.Services.AddHttpClient<JobApiClient>(client =>
        {
            client.BaseAddress = new Uri(serverConfig.LocalhostUrl);
        });

        builder.Services.AddHttpClient<INtfyNotificationService, NtfyNotificationService>();

        var retryPolicy = Policy<bool>
            .Handle<Exception>()
            .OrResult(r => !r)
            .WaitAndRetryAsync(
                retryCount: resilienceOptions.RetryAttempts,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(resilienceOptions.BackoffBaseSeconds, attempt)),
                onRetry: (outcome, timespan, retryCount, _) =>
                {
                    Log.Warning("Polly retry triggered. Attempt {RetryCount} after {Delay}ms. Exception: {Exception}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result.ToString());
                });

        var circuitBreakerPolicy = Policy<bool>
            .Handle<Exception>()
            .OrResult(r => !r)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: resilienceOptions.CircuitBreakerThreshold,
                durationOfBreak: TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerDurationSeconds),
                onBreak: (_, timespan) =>
                {
                    Log.Error("Circuit breaker opened due to repeated failures. Breaking for {Duration}ms", timespan.TotalMilliseconds);
                },
                onReset: () =>
                {
                    Log.Information("Circuit breaker reset - service recovered");
                });

        var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        builder.Services.AddSingleton(retryPolicy);
        builder.Services.AddSingleton(circuitBreakerPolicy);
        builder.Services.AddSingleton(combinedPolicy);

        builder.WebHost.UseUrls(serverConfig.LocalhostUrl);

        builder.Services.AddSingleton<ISecurityOptions>(sp => sp.GetRequiredService<IOptions<SecurityOptions>>().Value);
        builder.Services.AddSingleton<IServerOptions>(sp => sp.GetRequiredService<IOptions<ServerOptions>>().Value);
        builder.Services.AddSingleton<INtfyOptions>(sp => sp.GetRequiredService<IOptions<NtfyOptions>>().Value);

        builder.Services.AddSingleton<IJobCache, JobCacheService>();
        builder.Services.AddSingleton<IJobRepository, JobRepository>();
        builder.Services.AddSingleton<IJobQueryService, JobQueryService>();
        builder.Services.AddSingleton<IJobStateTransitionService, JobStateTransitionService>();
        builder.Services.AddSingleton<IJobRunService, JobRunService>();
        builder.Services.AddSingleton<IJobStateManager, JobStateManager>();

        builder.Services.AddSingleton<IJobValidationService, JobValidationService>();
        builder.Services.AddSingleton<IJobStatusService, JobStatusService>();

        builder.Services.AddSingleton<IJobRunner, JobRunner>();
        builder.Services.AddScoped<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IAuditLogService, AuditLogService>();
        builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        builder.Services.AddHostedService<BackgroundTaskQueue>(sp => sp.GetRequiredService<IBackgroundTaskQueue>() as BackgroundTaskQueue
            ?? throw new InvalidOperationException("BackgroundTaskQueue not registered"));
        builder.Services.AddHostedService<SchedulerService>();
        builder.Services.AddHostedService<JobHealthCheckService>();

        builder.Services.AddControllers();
        builder.Services.AddRazorComponents(options =>
        {
            options.DetailedErrors = builder.Environment.IsDevelopment();
        }).AddInteractiveServerComponents();

        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "AutomatedTaskSchedulerService";
        });

        builder.Services.AddSignalR();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(serverConfig.LoopbackUrl, serverConfig.LocalhostUrl)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        builder.Services.AddSingleton<DatabaseConnectionValidator>();

        return new StartupConfiguration(databaseOptions, serverConfig, resilienceOptions, connectionString);
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
}

