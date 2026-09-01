using AutomationEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Periodic health check service that validates running jobs have valid processes
    /// Prevents jobs from staying stuck in Running state indefinitely
    /// </summary>
    public class JobHealthCheckService : BackgroundService
    {
        private readonly IJobStateTransitionService _stateTransition;
        private readonly ILogger<JobHealthCheckService> _logger;
        private readonly TimeSpan _checkInterval;

        public JobHealthCheckService(
            IJobStateTransitionService stateTransition,
            ILogger<JobHealthCheckService> logger,
            TimeSpan? checkInterval = null)
        {
            _stateTransition = stateTransition;
            _logger = logger;
            // Default to 5 minutes if not specified
            _checkInterval = checkInterval ?? TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Run periodic health checks on background thread
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job health check service started | CheckInterval: {CheckInterval} minutes", 
                _checkInterval.TotalMinutes);

            // Initial delay to allow system to initialize
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Running job health check");
                    var stopwatch = Stopwatch.StartNew();

                    // Validate all running jobs have valid processes
                    await _stateTransition.ValidateAndCleanupStaleRunningStatesAsync();

                    stopwatch.Stop();
                    _logger.LogDebug("Job health check completed | DurationMs: {DurationMs}", stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during job health check");
                    // Continue running despite errors
                }

                try
                {
                    // Wait for next check interval
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when service is stopping
                    break;
                }
            }

            _logger.LogInformation("Job health check service stopped");
        }
    }
}
