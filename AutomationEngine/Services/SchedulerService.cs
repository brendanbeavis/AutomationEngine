using Cronos;
using AutomationEngine.Application.Abstractions;
using AutomationEngine.Data.Entities;
using AutomationEngine.Infrastructure.Observability;
using AutomationEngine.Models;
using AutomationEngine.Options;
using AutomationEngine.Application.Abstractions;
using AutomationEngine.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutomationEngine.Services
{
    public class SchedulerService : BackgroundService
    {
        private readonly ILogger<SchedulerService> _logger;
        private readonly IServiceProvider _services;
        private readonly IJobStateManager _stateManager;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IHubContext<JobStatusHub> _hubContext;
        private readonly SchedulerOptions _schedulerOptions;
        private readonly List<(JobEntity job, CronExpression? cron, TimeZoneInfo tz)> _jobs = new();

        public SchedulerService(ILogger<SchedulerService> logger, IServiceProvider services,
            IJobStateManager stateManager, IBackgroundTaskQueue backgroundTaskQueue, IHubContext<JobStatusHub> hubContext,
            IOptions<SchedulerOptions> schedulerOptions)
        {
            _logger = logger;
            _services = services;
            _stateManager = stateManager;
            _backgroundTaskQueue = backgroundTaskQueue;
            _hubContext = hubContext;
            _schedulerOptions = schedulerOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Load jobs from database on startup
            await _stateManager.LoadJobsAsync();
            ReloadJobsFromState();

            StructuredLogger.LogSchedulerEvent(_logger, "ServiceStarted", "", $"Scheduler started with {_jobs.Count} jobs");

            var nextRunTimes = new Dictionary<string, DateTimeOffset?>();
            foreach (var (job, cron, tz) in _jobs)
            {
                nextRunTimes[job.JobId] = cron?.GetNextOccurrence(DateTimeOffset.Now, tz);
            }

            // Reload jobs periodically to pick up changes
            var reloadInterval = TimeSpan.FromSeconds(_schedulerOptions.ReloadIntervalSeconds);
            var lastReload = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.Now;
                foreach (var (job, cron, tz) in _jobs)
                {
                    if (!job.Enabled) continue;

                    var next = nextRunTimes[job.JobId];
                    if (next == null || next > now) continue;

                    try
                    {
                        // Set job state to Running so dashboard immediately shows it as running
                        await _stateManager.SetJobStateAsync(job.JobId, JobState.Running);

                        // Broadcast job started event to connected clients via SignalR
                        await _hubContext.BroadcastJobStartedAsync(job.JobId, job.DisplayName);

                        // Broadcast jobs list updated to trigger dashboard refresh
                        await _hubContext.BroadcastJobsListUpdatedAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update job state or broadcast started event | JobId: {JobId} | DisplayName: {DisplayName}", 
                            job.JobId, job.DisplayName);
                    }

                    // Queue job in background task queue for safe execution with proper exception handling
                    await _backgroundTaskQueue.QueueAsync(job.JobId, ct => RunJobAsync(job, ct));

                    // compute next occurrence
                    nextRunTimes[job.JobId] = cron?.GetNextOccurrence(now.AddSeconds(1), tz);
                }

                // Reload jobs from database periodically
                if (DateTime.UtcNow - lastReload > reloadInterval)
                {
                    await _stateManager.LoadJobsAsync();
                    ReloadJobsFromState();
                    lastReload = DateTime.UtcNow;

                    // Reinitialize next run times for new jobs
                    foreach (var (job, cron, tz) in _jobs.Where(j => !nextRunTimes.ContainsKey(j.job.JobId)))
                    {
                        nextRunTimes[job.JobId] = cron?.GetNextOccurrence(now, tz);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_schedulerOptions.LoopDelaySeconds), stoppingToken).ConfigureAwait(false);
            }
        }

        private void ReloadJobsFromState()
        {
            _logger.LogDebug("Reloading jobs from state manager");
            _jobs.Clear();
            var allJobs = _stateManager.GetAllJobs();
            _logger.LogInformation("Loaded {JobCount} jobs from state manager", allJobs.Count);

            foreach (var job in allJobs)
            {
                CronExpression? cron = null;
                try
                {
                    cron = CronExpression.Parse(job.Schedule, CronFormat.Standard);
                    _logger.LogDebug("Parsed cron schedule successfully | JobId: {JobId} | Schedule: {Schedule}", 
                        job.JobId, job.Schedule);
                }
                catch (Exception ex)
                {
                    //TODO, log this to audit log, and mark the job as invalid in the database
                    _logger.LogWarning(ex, "Failed to parse cron schedule | JobId: {JobId} | Schedule: {Schedule}", 
                        job.JobId, job.Schedule);
                }

                _jobs.Add((job, cron, TimeZoneInfo.Local));
            }

            _logger.LogDebug("Scheduler now tracking {JobCount} enabled jobs", _jobs.Count(j => j.job.Enabled));
        }

        private async ValueTask RunJobAsync(JobEntity job, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Scheduler triggering job execution | JobId: {JobId} | DisplayName: {DisplayName}", 
                job.JobId, job.DisplayName);

            try
            {
                using var scope = _services.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IJobRunner>();

                // Convert JobEntity to JobConfig for runner. Keep mapping similar to existing schema and read persisted Type/Script.
                var jobConfig = new JobConfig
                {
                    Id = job.JobId,
                    DisplayName = job.DisplayName,
                    Command = job.Command,
                    Arguments = job.Arguments,
                    Script = job.Script,
                    Type = job.Type,
                    WorkingDirectory = job.WorkingDirectory,
                    Schedule = job.Schedule,
                    TimeoutSeconds = job.TimeoutSeconds,
                    Retry = job.Retry,
                    SuccessExitCodes = job.SuccessExitCodes,
                    OnFailureNotify = job.OnFailureNotify,
                    OnSuccessNotify = job.OnSuccessNotify,
                    TargetFolder = job.TargetFolder,
                    FileAgeInDays = job.FileAgeInDays,
                    Recurse = job.Recurse,
                    FileFilter = job.FileFilter
                };

                // Run the job once. Retries are handled inside JobRunner via Polly.
                var result = await runner.RunAsync(jobConfig, cancellationToken).ConfigureAwait(false);

                // Save run result to database
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Job execution completed | JobId: {JobId} | DisplayName: {DisplayName} | Success: {Success} | Duration: {Duration}ms", 
                    job.JobId, job.DisplayName, result.Success, duration.TotalMilliseconds);

                await _stateManager.SaveJobRunAsync(job.JobId, result, startTime, duration);
                await _hubContext.BroadcastJobCompletedAsync(
                   job.JobId,
                   result.Success,
                   result.ExitCode,
                   result.StdErr,
                   (int)duration.TotalMilliseconds).ConfigureAwait(false);

                await _hubContext.BroadcastJobsListUpdatedAsync().ConfigureAwait(false);

                if ((!result.Success && job.OnFailureNotify) || (result.Success && job.OnSuccessNotify))
                {
                    try
                    {
                        var ntfyService = scope.ServiceProvider.GetRequiredService<INtfyNotificationService>();

                        _logger.LogDebug("Sending notification | JobId: {JobId} | Type: {NotificationType}",
                            job.JobId, result.Success ? "Success" : "Failure");

                        if (result.Success)
                        {
                            await ntfyService.SendSuccessNotificationAsync(
                                job.DisplayName,
                                string.IsNullOrWhiteSpace(result.StdOut)
                                    ? $"Exit code: {result.ExitCode}"
                                    : result.StdOut,
                                cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await ntfyService.SendFailureNotificationAsync(
                                job.DisplayName,
                                result.StdErr ?? "No error details available",
                                cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send ntfy notification | JobId: {JobId} | DisplayName: {DisplayName}", 
                            job.JobId, job.DisplayName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduler failed running job | JobId: {JobId} | DisplayName: {DisplayName}", 
                    job.JobId, job.DisplayName);

                var failedResult = new JobResult
                {
                    Success = false,
                    ExitCode = -1,
                    StdErr = ex.Message,
                    StdOut = string.Empty
                };

                var duration = DateTime.UtcNow - startTime;
                await _stateManager.SaveJobRunAsync(job.JobId, failedResult, startTime, duration);

                await _hubContext.BroadcastJobCompletedAsync(job.JobId, false, -1, ex.Message, (int)duration.TotalMilliseconds).ConfigureAwait(false);
                await _hubContext.BroadcastJobsListUpdatedAsync().ConfigureAwait(false);
            }
        }
    }
}

