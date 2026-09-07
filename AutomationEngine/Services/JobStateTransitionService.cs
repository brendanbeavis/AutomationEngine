using AutomationEngine.Data.Entities;
using AutomationEngine.Models;
using AutomationEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Service for managing job state transitions
    /// Single Responsibility: State machine logic only
    /// No database access, uses injected repository for persistence
    /// </summary>
    public class JobStateTransitionService : IJobStateTransitionService
    {
        private readonly IJobRepository _repository;
        private readonly IJobCache _cache;
        private readonly IAuditLogService _auditService;
        private readonly ILogger<JobStateTransitionService> _logger;

        public JobStateTransitionService(
            IJobRepository repository,
            IJobCache cache,
            IAuditLogService auditService,
            ILogger<JobStateTransitionService> logger)
        {
            _repository = repository;
            _cache = cache;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task TransitionToStateAsync(string jobId, JobState newState, int? processId = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var job = _cache.Get(jobId);
                if (job is null)
                {
                    _logger.LogWarning("Job not found for state transition | JobId: {JobId} | NewState: {NewState}", 
                        jobId, newState);
                    return;
                }

                var oldState = job.CurrentState;

                // Validate transition is allowed
                if (!IsValidTransition(oldState, newState))
                {
                    _logger.LogWarning("Invalid state transition blocked | JobId: {JobId} | From: {OldState} | To: {NewState}", 
                        jobId, oldState, newState);
                    return;
                }

                // Update state and running metadata
                job.CurrentState = newState;
                job.UpdatedAt = DateTime.UtcNow;

                if (newState == JobState.Running)
                {
                    job.RunningStartTime = DateTime.UtcNow;
                    job.RunningProcessId = processId;
                }
                else if (oldState == JobState.Running)
                {
                    job.RunningStartTime = null;
                    job.RunningProcessId = null;
                }

                // Persist to database
                await _repository.UpdateJobAsync(job);

                // Update cache
                _cache.Set(jobId, job);

                stopwatch.Stop();
                StructuredLogger.LogJobStateTransition(_logger, jobId, oldState.ToString(), newState.ToString(),
                    $"Transitioned to {newState}, ProcessId: {processId ?? -1}");

                // Log to audit
                string auditAction = newState switch
                {
                    JobState.Running => AuditLogActions.JobStarted,
                    JobState.Stopped => AuditLogActions.JobStopped,
                    JobState.Completed => AuditLogActions.JobCompleted,
                    JobState.Failed => AuditLogActions.JobFailed,
                    _ => "JobStateChanged"
                };

                await _auditService.LogAuditEventAsync(
                    job.Id,
                    auditAction,
                    $"Job state changed from {oldState} to {newState}",
                    null,
                    null,
                    null,
                    null,
                    new { oldState = oldState.ToString(), newState = newState.ToString(), processId }
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to transition job state | JobId: {JobId} | NewState: {NewState} | ErrorTimeMs: {ErrorTimeMs}",
                    jobId, newState, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task TransitionToRunningAsync(string jobId, int processId)
        {
            await TransitionToStateAsync(jobId, JobState.Running, processId);
        }

        public async Task TransitionToCompletedAsync(string jobId)
        {
            await TransitionToStateAsync(jobId, JobState.Completed);
        }

        public async Task TransitionToFailedAsync(string jobId)
        {
            await TransitionToStateAsync(jobId, JobState.Failed);
        }

        public async Task TransitionToStoppedAsync(string jobId)
        {
            await TransitionToStateAsync(jobId, JobState.Stopped);
        }

        public async Task<JobState?> GetCurrentStateAsync(string jobId)
        {
            var job = _cache.Get(jobId);
            if (job == null)
            {
                _logger.LogDebug("Job not found in cache for state query | JobId: {JobId}", jobId);
                return null;
            }
            _logger.LogDebug("Retrieved job state | JobId: {JobId} | State: {State}", jobId, job.CurrentState);
            return job?.CurrentState;
        }

        public bool IsValidTransition(JobState currentState, JobState newState)
        {
            // Define valid state transitions
            var validTransitions = new Dictionary<JobState, List<JobState>>
            {
                { JobState.Idle, new List<JobState> { JobState.Running, JobState.Disabled } },
                { JobState.Running, new List<JobState> { JobState.Completed, JobState.Failed, JobState.Stopped } },
                { JobState.Completed, new List<JobState> { JobState.Idle, JobState.Running } },
                { JobState.Failed, new List<JobState> { JobState.Idle, JobState.Running } },
                { JobState.Stopped, new List<JobState> { JobState.Idle, JobState.Running } },
                { JobState.Disabled, new List<JobState> { JobState.Idle } }
            };

            return validTransitions.TryGetValue(currentState, out var nextStates) 
                && nextStates.Contains(newState);
        }

        public IEnumerable<JobState> GetAllowedNextStates(JobState currentState)
        {
            var validTransitions = new Dictionary<JobState, List<JobState>>
            {
                { JobState.Idle, new List<JobState> { JobState.Running, JobState.Disabled } },
                { JobState.Running, new List<JobState> { JobState.Completed, JobState.Failed, JobState.Stopped } },
                { JobState.Completed, new List<JobState> { JobState.Idle, JobState.Running } },
                { JobState.Failed, new List<JobState> { JobState.Idle, JobState.Running } },
                { JobState.Stopped, new List<JobState> { JobState.Idle, JobState.Running } },
                { JobState.Disabled, new List<JobState> { JobState.Idle } }
            };

            return validTransitions.TryGetValue(currentState, out var nextStates)
                ? nextStates
                : new List<JobState>();
        }

        /// <summary>
        /// Check if a process with the given ID still exists
        /// </summary>
        public bool IsProcessStillRunning(int processId)
        {
            try
            {
                var process = System.Diagnostics.Process.GetProcessById(processId);
                // Process exists and hasn't exited
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                // Process not found
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking process existence | ProcessId: {ProcessId}", processId);
                // If we can't determine, assume it's gone
                return false;
            }
        }

        /// <summary>
        /// Validate and cleanup stale Running states where the process no longer exists
        /// This prevents jobs from staying stuck in Running state indefinitely
        /// </summary>
        public async Task ValidateAndCleanupStaleRunningStatesAsync()
        {
            try
            {
                var runningJobs = _cache.GetAll()
                    .Where(j => j.CurrentState == JobState.Running && j.RunningProcessId.HasValue)
                    .ToList();

                if (!runningJobs.Any())
                {
                    _logger.LogDebug("No running jobs to validate");
                    return;
                }

                _logger.LogDebug("Validating {RunningJobCount} running jobs for stale processes", runningJobs.Count);

                foreach (var job in runningJobs)
                {
                    if (!IsProcessStillRunning(job.RunningProcessId.Value))
                    {
                        _logger.LogWarning("Detected stale Running state - process no longer exists | JobId: {JobId} | ProcessId: {ProcessId}",
                            job.JobId, job.RunningProcessId);

                        // Auto-cleanup: transition to Stopped state
                        job.CurrentState = JobState.Stopped;
                        job.RunningProcessId = null;
                        job.RunningStartTime = null;
                        job.UpdatedAt = DateTime.UtcNow;

                        await _repository.UpdateJobAsync(job);
                        _cache.Set(job.JobId, job);

                        StructuredLogger.LogJobStateTransition(_logger, job.JobId, "Running", "Stopped",
                            $"Auto-corrected from stale Running state - process {job.RunningProcessId} no longer exists");

                        // Log to audit
                        await _auditService.LogAuditEventAsync(
                            job.Id,
                            AuditLogActions.JobStopped,
                            "Job auto-corrected from stale Running state - process no longer exists",
                            null,
                            null,
                            null,
                            null,
                            new { reason = "ProcessNotFound", deadProcessId = job.RunningProcessId }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stale running state validation");
                // Don't throw - this is a background validation task
            }
        }
    }
}
