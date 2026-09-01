using AutomationEngine.Data.Entities;
using AutomationEngine.Extensions;
using AutomationEngine.Models;
using AutomationEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Service for managing job run results and history
    /// Single Responsibility: Job run tracking and statistics
    /// </summary>
    public class JobRunService : IJobRunService
    {
        private readonly IJobRepository _repository;
        private readonly IJobCache _cache;
        private readonly IJobStateTransitionService _stateTransitionService;
        private readonly IAuditLogService _auditService;
        private readonly ILogger<JobRunService> _logger;

        public JobRunService(
            IJobRepository repository,
            IJobCache cache,
            IJobStateTransitionService stateTransitionService,
            IAuditLogService auditService,
            ILogger<JobRunService> logger)
        {
            _repository = repository;
            _cache = cache;
            _stateTransitionService = stateTransitionService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task SaveJobRunAsync(string jobId, JobResult result, DateTime startedAt, TimeSpan duration)
        {
            try
            {
                var job = _cache.Get(jobId);
                if (job is null)
                {
                    _logger.LogWarning("Job {JobId} not found for saving run result", jobId);
                    return;
                }

                // Create run entity
                var run = new JobRunEntity
                {
                    JobId = job.Id,
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow,
                    Success = result.Success,
                    ExitCode = result.ExitCode,
                    StdOut = result.StdOut.Truncate(2000) ?? "",
                    StdErr = result.StdErr.Truncate(2000) ?? "",
                    DurationMs = (int)duration.TotalMilliseconds
                };

                // Save run to database
                await _repository.SaveJobRunAsync(run);

                // Update job state based on result
                var newState = result.Success ? JobState.Completed : JobState.Failed;
                job.CurrentState = newState;
                job.RunningStartTime = null;
                job.RunningProcessId = null;
                job.UpdatedAt = DateTime.UtcNow;

                // Persist job state to database
                await _repository.UpdateJobAsync(job);

                // Update cache
                _cache.Set(jobId, job);

                StructuredLogger.LogJobCompleted(_logger, jobId, "", duration, result.Success, result.StdErr);

                // Log to audit
                var action = result.Success ? AuditLogActions.JobCompleted : AuditLogActions.JobFailed;
                await _auditService.LogAuditEventAsync(
                    job.Id,
                    action,
                    $"Job execution {(result.Success ? "completed" : "failed")}",
                    result.Success,
                    result.ExitCode,
                    (int)duration.TotalMilliseconds,
                    result.StdErr?.Truncate(500),
                    new { exitCode = result.ExitCode, stdout_length = result.StdOut?.Length ?? 0, stderr_length = result.StdErr?.Length ?? 0 }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save job run for {JobId}", jobId);
            }
        }

        public async Task<List<JobRunEntity>> GetJobRunsAsync(string jobId, int limit = 50)
        {
            return await _repository.GetJobRunsAsync(jobId, limit);
        }

        public async Task<JobRunEntity?> GetJobRunByIdAsync(int runId)
        {
            // This would require additional repository method, but not implemented in original
            return await Task.FromResult<JobRunEntity?>(null);
        }

        public async Task<JobRunStatistics> GetJobRunStatisticsAsync(string jobId)
        {
            try
            {
                var runs = await GetJobRunsAsync(jobId, limit: 1000);

                var stats = new JobRunStatistics
                {
                    TotalRuns = runs.Count,
                    SuccessfulRuns = runs.Count(r => r.Success),
                    FailedRuns = runs.Count(r => !r.Success),
                    AverageDuration = runs.Any() ? TimeSpan.FromMilliseconds(runs.Average(r => (double)r.DurationMs)) : null,
                    LastRunTime = runs.FirstOrDefault()?.CompletedAt,
                    LastRunSuccess = runs.FirstOrDefault()?.Success ?? false
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job run statistics for {JobId}", jobId);
                return new JobRunStatistics();
            }
        }

        public async Task<int> DeleteOldJobRunsAsync(int retentionDays = 90)
        {
            // This would require additional repository method, but not implemented in original
            return await Task.FromResult(0);
        }
    }
}
