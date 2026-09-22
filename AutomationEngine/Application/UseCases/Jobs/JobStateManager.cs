using AutomationEngine.Api.Requests;
using AutomationEngine.Data.Entities;
using AutomationEngine.Models;
using AutomationEngine.Application.Abstractions;
using AutomationEngine.Infrastructure.Observability;
using AutomationEngine.Services;
using AutomationEngine.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AutomationEngine.Application.UseCases.Jobs
{
    /// <summary>
    /// Facade orchestrating job management services
    /// Delegates to specialized services following Single Responsibility Principle
    /// - IJobRepository: Data persistence
    /// - IJobCache: In-memory caching
    /// - IJobStateTransitionService: State machine logic
    /// - IJobRunService: Job run tracking
    /// - IJobQueryService: Query operations
    /// </summary>
    public class JobStateManager : IJobStateManager
    {
        private readonly IJobRepository _repository;
        private readonly IJobCache _cache;
        private readonly IJobStateTransitionService _stateTransition;
        private readonly IJobRunService _runService;
        private readonly IJobQueryService _queryService;
        private readonly IAuditLogService _auditService;
        private readonly ILogger<JobStateManager> _logger;

        public JobStateManager(
            IJobRepository repository,
            IJobCache cache,
            IJobStateTransitionService stateTransition,
            IJobRunService runService,
            IJobQueryService queryService,
            IAuditLogService auditService,
            ILogger<JobStateManager> logger)
        {
            _repository = repository;
            _cache = cache;
            _stateTransition = stateTransition;
            _runService = runService;
            _queryService = queryService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Load all active jobs from database into cache
        /// </summary>
        public async Task LoadJobsAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var jobs = await _repository.LoadAllActiveJobsAsync();
                await _cache.LoadAsync(jobs);
                stopwatch.Stop();
                StructuredLogger.LogDatabaseOperation(_logger, "LoadJobs", "JobEntity", jobs.Count, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var classification = ErrorClassifier.Classify(ex);
                _logger.LogError(ex, "Failed to load jobs | Classification: {Classification} | LoadTimeMs: {LoadTimeMs}",
                    classification, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Refresh a specific job from database into cache
        /// </summary>
        public async Task<JobEntity?> RefreshJobAsync(string jobId)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var job = await _repository.GetJobByIdAsync(jobId);
                if (job is not null)
                {
                    _cache.Set(jobId, job);
                    StructuredLogger.LogJobRefresh(_logger, jobId, true, stopwatch.Elapsed);
                }
                else
                {
                    _cache.Remove(jobId);
                    StructuredLogger.LogJobRefresh(_logger, jobId, false, stopwatch.Elapsed);
                }
                return job;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to refresh job cache | JobId: {JobId}", jobId);
                throw;
            }
        }

        /// <summary>
        /// Get all cached jobs
        /// </summary>
        public List<JobEntity> GetAllJobs()
        {
            return _queryService.GetAllJobs();
        }

        /// <summary>
        /// Get a single job by JobId
        /// </summary>
        public JobEntity? GetJob(string jobId)
        {
            return _queryService.GetJobById(jobId);
        }

        /// <summary>
        /// Add or update a job in database and cache
        /// </summary>
        public async Task<JobEntity> SaveJobAsync(SaveJobRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var isCreate = false;

            using (LoggingContext.CreateActivityScope(request.JobId, "JobSave"))
            {
                try
                {
                    var existing = await _repository.GetJobByIdAsync(request.JobId);
                    isCreate = existing is null;

                    if (existing is not null)
                    {
                        // Update existing job
                        _logger.LogInformation("Job update initiated | JobId: {JobId} | DisplayName: {DisplayName}",
                            request.JobId, request.DisplayName);

                        existing.DisplayName = request.DisplayName;
                        existing.Command = request.Command;
                        existing.Arguments = request.Arguments;
                        existing.WorkingDirectory = request.WorkingDirectory;
                        existing.Type = request.Type;
                        existing.Script = request.Script;
                        existing.Schedule = request.Schedule;
                        existing.TimeoutSeconds = request.TimeoutSeconds;
                        existing.Retry = request.Retry;
                        existing.SuccessExitCodes = request.SuccessExitCodes;
                        existing.OnFailureNotify = request.OnFailureNotify;
                        existing.OnSuccessNotify = request.OnSuccessNotify;
                        existing.Enabled = request.Enabled;
                        existing.TargetFolder = request.FileCleanup?.TargetFolder;
                        existing.FileAgeInDays = request.FileCleanup?.FileAgeInDays ?? 0;
                        existing.Recurse = request.FileCleanup?.Recurse ?? false;
                        existing.FileFilter = request.FileCleanup?.FileFilter;
                        existing.UpdatedAt = DateTime.UtcNow;

                        StructuredLogger.LogJobStateTransition(_logger, request.JobId, "Existing", "Updated",
                            $"Updated by SaveJobAsync: {request.DisplayName}");

                        existing = await _repository.UpdateJobAsync(existing);
                    }
                    else
                    {
                        // Create new job
                        _logger.LogInformation("Job creation initiated | JobId: {JobId} | DisplayName: {DisplayName}",
                            request.JobId, request.DisplayName);

                        existing = new JobEntity
                        {
                            JobId = request.JobId,
                            DisplayName = request.DisplayName,
                            Command = request.Command,
                            Arguments = request.Arguments,
                            WorkingDirectory = request.WorkingDirectory,
                            Type = request.Type,
                            Script = request.Script,
                            Schedule = request.Schedule,
                            TimeoutSeconds = request.TimeoutSeconds,
                            Retry = request.Retry,
                            SuccessExitCodes = request.SuccessExitCodes,
                            OnFailureNotify = request.OnFailureNotify,
                            OnSuccessNotify = request.OnSuccessNotify,
                            Enabled = request.Enabled,
                            TargetFolder = request.FileCleanup?.TargetFolder,
                            FileAgeInDays = request.FileCleanup?.FileAgeInDays ?? 0,
                            Recurse = request.FileCleanup?.Recurse ?? false,
                            FileFilter = request.FileCleanup?.FileFilter,
                            CreatedAt = DateTime.UtcNow
                        };

                        StructuredLogger.LogJobStateTransition(_logger, request.JobId, "New", "Created", request.DisplayName);
                        existing = await _repository.CreateJobAsync(existing);
                    }

                    // Update cache
                    _cache.Set(request.JobId, existing);

                    stopwatch.Stop();
                    var operation = isCreate ? "CreateJob" : "UpdateJob";
                    StructuredLogger.LogDatabaseOperation(_logger, operation, "JobEntity", 1, stopwatch.Elapsed);
                    StructuredLogger.LogJobSaveOperation(_logger, request.JobId, request.DisplayName, isCreate, stopwatch.Elapsed);

                    // Log to audit
                    var action = isCreate ? AuditLogActions.JobCreated : AuditLogActions.JobUpdated;
                    var description = isCreate
                        ? $"Job created: {request.DisplayName} (Type: {request.Type})"
                        : $"Job updated: {request.DisplayName} (Type: {request.Type})";

                    await _auditService.LogAuditEventAsync(
                        existing.Id,
                        action,
                        description,
                        null,
                        null,
                        null,
                        null,
                        new { type = request.Type.ToString(), schedule = request.Schedule, enabled = request.Enabled, timeout = request.TimeoutSeconds }
                    );

                    return existing;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var classification = ErrorClassifier.Classify(ex);
                    _logger.LogError(ex, "Failed to save job | JobId: {JobId} | Classification: {Classification}",
                        request.JobId, classification);
                    throw;
                }
            }
        }

        public Task<JobEntity> SaveJobAsync(
            string jobId,
            string displayName,
            string? command,
            string? arguments,
            string? workingDirectory,
            JobType type,
            string? script,
            string schedule,
            int timeoutSeconds,
            int retry,
            bool onFailureNotify,
            bool enabled,
            string? successExitCodes = null,
            bool onSuccessNotify = false,
            string? targetFolder = null,
            int fileAgeInDays = 0,
            bool recurse = false,
            string? fileFilter = null)
        {
            FileCleanupOptions? fileCleanup = null;
            if (type == JobType.FileCleanup)
            {
                fileCleanup = new FileCleanupOptions
                {
                    TargetFolder = targetFolder,
                    FileAgeInDays = fileAgeInDays,
                    Recurse = recurse,
                    FileFilter = fileFilter
                };
            }

            var request = new SaveJobRequest
            {
                JobId = jobId,
                DisplayName = displayName,
                Command = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                Type = type,
                Script = script,
                Schedule = schedule,
                TimeoutSeconds = timeoutSeconds,
                Retry = retry,
                SuccessExitCodes = successExitCodes,
                OnFailureNotify = onFailureNotify,
                OnSuccessNotify = onSuccessNotify,
                Enabled = enabled,
                FileCleanup = fileCleanup
            };

            return SaveJobAsync(request);
        }

        /// <summary>
        /// Delete a job (soft delete)
        /// </summary>
        public async Task DeleteJobAsync(string jobId)
        {
            var stopwatch = Stopwatch.StartNew();

            using (LoggingContext.CreateActivityScope(jobId, "JobDelete"))
            {
                try
                {
                    _logger.LogInformation("Job soft-deletion initiated | JobId: {JobId}", jobId);
                    await _repository.DeleteJobAsync(jobId);

                    // Remove from cache
                    _cache.Remove(jobId);

                    stopwatch.Stop();
                    StructuredLogger.LogJobStateTransition(_logger, jobId, "Active", "Deleted", "Soft deletion");
                    StructuredLogger.LogJobDeletion(_logger, jobId, true, null, stopwatch.Elapsed);

                    // Log to audit
                    var job = await _repository.GetJobByIdAsync(jobId);
                    if (job is not null)
                    {
                        await _auditService.LogAuditEventAsync(
                            job.Id,
                            AuditLogActions.JobDeleted,
                            "Job soft-deleted",
                            null,
                            null,
                            null,
                            null,
                            null
                        );
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    StructuredLogger.LogJobDeletion(_logger, jobId, false);
                    throw;
                }
            }
        }

        /// <summary>
        /// Save a job run result
        /// </summary>
        public async Task SaveJobRunAsync(string jobId, JobResult result, DateTime startedAt, TimeSpan duration)
        {
            await _runService.SaveJobRunAsync(jobId, result, startedAt, duration);
        }

        /// <summary>
        /// Get job runs for a specific job
        /// </summary>
        public async Task<List<JobRunEntity>> GetJobRunsAsync(string jobId, int limit = 50)
        {
            return await _runService.GetJobRunsAsync(jobId, limit);
        }

        /// <summary>
        /// Set job enabled/disabled status
        /// </summary>
        public async Task SetJobEnabledAsync(string jobId, bool enabled)
        {
            try
            {
                var job = _cache.Get(jobId);
                if (job is not null)
                {
                    job.Enabled = enabled;
                    job.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateJobAsync(job);
                    _cache.Set(jobId, job);

                    StructuredLogger.LogJobStateTransition(_logger, jobId, enabled ? "Disabled" : "Enabled",
                        enabled ? "Enabled" : "Disabled", "Job enabled state changed");

                    // Log to audit
                    var action = enabled ? AuditLogActions.JobEnabled : AuditLogActions.JobDisabled;
                    await _auditService.LogAuditEventAsync(
                        job.Id,
                        action,
                        $"Job {(enabled ? "enabled" : "disabled")}",
                        null,
                        null,
                        null,
                        null,
                        new { enabled }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set job enabled state | JobId: {JobId}", jobId);
                throw;
            }
        }

        /// <summary>
        /// Get all running jobs of a specific type
        /// </summary>
        public List<JobEntity> GetRunningJobsByType(JobType jobType)
        {
            return _queryService.GetRunningJobsByType(jobType);
        }

        /// <summary>
        /// Check if a job type is currently busy
        /// </summary>
        public bool IsJobBusy(int id)
        {
            return _queryService.IsJobBusy(id);
        }

        /// <summary>
        /// Set the state of a job
        /// </summary>
        public async Task SetJobStateAsync(string jobId, JobState newState, int? processId = null)
        {
            await _stateTransition.TransitionToStateAsync(jobId, newState, processId);
        }

        /// <summary>
        /// Get all currently running jobs
        /// </summary>
        public List<JobEntity> GetAllRunningJobs()
        {
            return _queryService.GetAllRunningJobs();
        }

        /// <summary>
        /// Get a job's current running duration
        /// </summary>
        public TimeSpan? GetJobRunningDuration(string jobId)
        {
            return _queryService.GetJobRunningDuration(jobId);
        }
    }
}

