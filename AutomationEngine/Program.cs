using AutomationEngine.Api;
using AutomationEngine.Configuration;
using AutomationEngine.Data;
using AutomationEngine.Services;
using AutomationEngine.Services.Abstractions;
using AutomationEngine.SignalR;
using AutomationEngine.Middleware;
using AutomationEngine.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Context;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using AutomationEngine.Options.Interfaces;

// Configure Serilog with structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AutomationEngine")
    .CreateLogger();

try
{
    var exePath = Assembly.GetExecutingAssembly().Location;
    var exeDirectory = Path.GetDirectoryName(exePath);
    Directory.SetCurrentDirectory(exeDirectory);
    Log.Debug("Application working directory set to {WorkingDirectory}", exeDirectory);
}
catch (Exception ex)
{
    Log.Warning(ex, "Failed to set working directory, continuing with default");
}

var builder = WebApplication.CreateBuilder(args);
Log.Information("WebApplication builder created");
//var builder = WebApplication.CreateBuilder(new WebApplicationOptions
//{
 //   Args = args,
 //   ContentRootPath = exeDirectory, // Hard-bind it directly during instantiation
//    ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
//});

builder.Host
    .UseWindowsService()
    .UseSerilog();

Log.Information("Host configuration: WindowsService enabled, Serilog logging configured");

// WINDOWS SERVICE CONFIGURATION:
// The .UseWindowsService() call enables the application to run as a Windows service.
// It is safe to call even when running as a console application - it will detect and work appropriately.
// When deployed as a Windows service:
// - Service Name: AutomatedTaskSchedulerService
// - The app will receive service lifecycle events (start, stop, pause, continue)
// - Graceful shutdown is handled automatically by the host
// - See docs/WINDOWS_SERVICE_SETUP.md for installation and management procedures

// Add services
Log.Debug("Configuring typed options from appsettings with startup validation");
builder.Services.AddAutomationEngineOptions(builder.Configuration);

static void ValidateOptionsObject<T>(T options, string optionsName)
{
    var context = new ValidationContext(options!);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(options!, context, results, validateAllProperties: true))
    {
        var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
        throw new InvalidOperationException($"{optionsName} configuration is invalid: {errors}");
    }
}

// Validate database configuration on startup
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

// Build connection string using validated options
var connectionString = ConnectionStringBuilder.Build(databaseOptions);
Log.Information("Database configured: Provider={Provider}, Timeout={TimeoutSec}s, Pool={PoolSize}, WAL={Wal}",
    databaseOptions.Provider, databaseOptions.CommandTimeoutSeconds, databaseOptions.PoolSize, databaseOptions.SqliteEnableWal);

// Use DbContextFactory for background job execution with singleton services
// This allows thread-safe DbContext instances without manual scoping
Log.Debug("Registering DbContextFactory for AutomationDbContext");
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

// Add HttpClient for NtfyNotificationService
//builder.Services.AddHttpClient<NtfyNotificationService>();
builder.Services.AddHttpClient<INtfyNotificationService, NtfyNotificationService>();

// Configure Polly resilience policies
Log.Debug("Configuring Polly resilience policies");
var retryPolicy = Policy<bool>
    .Handle<Exception>()
    .OrResult(r => !r)
    .WaitAndRetryAsync(
        retryCount: resilienceOptions.RetryAttempts,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(resilienceOptions.BackoffBaseSeconds, attempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
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
        onBreak: (outcome, timespan) =>
        {
            Log.Error("Circuit breaker opened due to repeated failures. Breaking for {Duration}ms", timespan.TotalMilliseconds);
        },
        onReset: () =>
        {
            Log.Information("Circuit breaker reset - service recovered");
        });

// Combine policies with wrap
var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
Log.Debug("Resilience policies configured and registered");

// Register policies as singletons for dependency injection
builder.Services.AddSingleton(retryPolicy);
builder.Services.AddSingleton(circuitBreakerPolicy);
builder.Services.AddSingleton(combinedPolicy);

// Configure Kestrel to listen on the URL derived from Server:Port
builder.WebHost.UseUrls(serverConfig.LocalhostUrl);
Log.Debug("Kestrel configured to listen on {Url}", serverConfig.LocalhostUrl);


// Configure typed options from configuration sections
Log.Debug("Configuring SecurityOptions and ServerOptions");
builder.Services.AddSingleton<ISecurityOptions>(sp => sp.GetRequiredService<IOptions<SecurityOptions>>().Value);

builder.Services.AddSingleton<IServerOptions>(sp => sp.GetRequiredService<IOptions<ServerOptions>>().Value);
builder.Services.AddSingleton<INtfyOptions>(sp => sp.GetRequiredService<IOptions<NtfyOptions>>().Value);

// Register job management services following Single Responsibility Principle
Log.Debug("Registering core job management services");
builder.Services.AddSingleton<IJobCache, JobCacheService>();
builder.Services.AddSingleton<IJobRepository, JobRepository>();
builder.Services.AddSingleton<IJobQueryService, JobQueryService>();
builder.Services.AddSingleton<IJobStateTransitionService, JobStateTransitionService>();
builder.Services.AddSingleton<IJobRunService, JobRunService>();
builder.Services.AddSingleton<JobStateManager>();

// Register job validation and status services
Log.Debug("Registering job validation and status services");
builder.Services.AddSingleton<IJobValidationService, JobValidationService>();
builder.Services.AddSingleton<IJobStatusService, JobStatusService>();

builder.Services.AddSingleton<JobRunner>();
Log.Debug("Registering settings and audit services");
builder.Services.AddScoped<ISettingsService, SettingsService>();
//builder.Services.AddScoped<INtfyNotificationService, NtfyNotificationService>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<BackgroundTaskQueue>(sp => sp.GetRequiredService<IBackgroundTaskQueue>() as BackgroundTaskQueue ?? throw new InvalidOperationException("BackgroundTaskQueue not registered"));
Log.Debug("Registering hosted services: SchedulerService, JobHealthCheckService");
builder.Services.AddHostedService<SchedulerService>();

// Periodic health check service for validating running jobs have valid processes
// Runs every 5 minutes to detect and auto-correct stale Running states
builder.Services.AddHostedService<JobHealthCheckService>();

// ASP.NET Core services
Log.Debug("Configuring ASP.NET Core services: Controllers, Razor Components, SignalR");
builder.Services.AddControllers();
builder.Services.AddRazorComponents(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
})
    .AddInteractiveServerComponents();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "AutomatedTaskSchedulerService";
});


builder.Services.AddSignalR();

// Configure CORS - Not needed for localhost-only app, but explicitly disabled for clarity
// Cross-origin requests are blocked at middleware level anyway
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

// Configure Kestrel to listen on localhost:5000
//builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
//{
//    options.ListenLocalhost(5000);
//});

////builder.Host.UseWindowsService();
//builder.Environment.ContentRootPath = exeDirectory;

// Register database connection validator
builder.Services.AddSingleton<DatabaseConnectionValidator>();

var app = builder.Build();


// Validate database connection on startup (non-fatal)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var validator = scope.ServiceProvider.GetRequiredService<DatabaseConnectionValidator>();
        var runtimeDatabaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (runtimeDatabaseOptions.ValidateConnectionOnStartup)
        {
            await validator.ValidateAndLogAsync();
        }
    }
}
catch (Exception ex)
{
    Log.Warning(ex, "Database validation encountered an error but will continue");
}

// Apply migrations on startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to apply database migrations - application cannot continue");
    throw;
}

// Initialize JobStateManager and validate running jobs
try
{
    using (var scope = app.Services.CreateScope())
    {
        var stateManager = scope.ServiceProvider.GetRequiredService<JobStateManager>();
        var stateTransition = scope.ServiceProvider.GetRequiredService<IJobStateTransitionService>();

        await stateManager.LoadJobsAsync();
        Log.Information("JobStateManager initialized successfully");

        // Validate all running jobs have valid processes on startup
        // This prevents issues where a job stays in Running state after app restart
        await stateTransition.ValidateAndCleanupStaleRunningStatesAsync();
        Log.Information("Startup validation of running jobs completed");
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to initialize JobStateManager - application cannot continue");
    throw;
}

// Configure the HTTP request pipeline
// Add localhost-only middleware to protect all subsequent requests
app.UseMiddleware<LocalhostOnlyMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

//app.UseHttpsRedirection();
//app.UseAntiforgery();

// Map endpoints
app.MapControllers();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseAntiforgery();
// Map static framework assets (required for Razor Components interactive render modes)
//app.MapStaticAssets();


app.MapRazorComponents<AutomationEngine.Components.App>()
    .AddInteractiveServerRenderMode();
app.MapHub<JobStatusHub>(Constants.SignalR.JobStatusHubPath);

// Log that the app is running
app.Lifetime.ApplicationStarted.Register(() =>
{
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
    Log.Information("Application started with version {Version} in environment {Environment}", version, app.Environment.EnvironmentName);

    // Get server options to log the actual configured URL
    var serverOptions = app.Services.GetRequiredService<IServerOptions>();
    Log.Information("Automation Engine started and ready to accept connections | Url: {Url}", serverOptions.LocalhostUrl);
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application shutdown requested");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Application has stopped - normal shutdown");
});

await app.RunAsync();
