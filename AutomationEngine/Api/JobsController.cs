using AutomationEngine.Api.Requests;
using AutomationEngine.Data.Entities;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Services;
using AutomationEngine.Services.Abstractions;
using AutomationEngine.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutomationEngine.Api
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly JobStateManager _stateManager;
        private readonly JobRunner _runner;
        private readonly IHubContext<JobStatusHub> _hubContext;
        private readonly ILogger<JobsController> _logger;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IAuditLogService _auditService;
        private readonly IJobValidationService _jobValidationService;
        private readonly IJobStatusService _jobStatusService;
        private readonly INtfyNotificationService _ntfyService;

        public JobsController(
            JobStateManager stateManager, 
            JobRunner runner, 
            IHubContext<JobStatusHub> hubContext,
            ILogger<JobsController> logger,
            IBackgroundTaskQueue backgroundTaskQueue,
            IAuditLogService auditService,
            IJobValidationService jobValidationService,
            IJobStatusService jobStatusService,
            INtfyNotificationService ntfyService)
        {
            _stateManager = stateManager;
            _runner = runner;
            _hubContext = hubContext;
            _logger = logger;
            _backgroundTaskQueue = backgroundTaskQueue;
            _auditService = auditService;
            _jobValidationService = jobValidationService;
            _jobStatusService = jobStatusService;
            _ntfyService = ntfyService;
        }

        /// <summary>
        /// Get all jobs
        /// </summary>
        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult GetAllJobs()
        {
            var jobs = _stateManager.GetAllJobs();
            var dto = jobs.Select(j => ToDto(j)).ToList();
            return Ok(dto);
        }

        /// <summary>
        /// Get a single job by ID
        /// </summary>
        [HttpGet("{jobId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public IActionResult GetJob(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("JobId cannot be empty");
            }

            // Validate JobId format
            if (!System.Text.RegularExpressions.Regex.IsMatch(jobId, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest("Invalid JobId format");
            }

            var job = _stateManager.GetJob(jobId);
            return job is null 
                ? NotFound(new { error = $"Job '{jobId}' not found" })
                : Ok(ToDto(job));
        }

        /// <summary>
        /// Check if a job with the given ID exists
        /// </summary>
        [HttpGet("{jobId}/exists")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult JobIdExists(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("JobId cannot be empty");
            }

            var job = _stateManager.GetJob(jobId);
            return Ok(job != null);
        }

        /// <summary>
        /// Create or update a job
        /// </summary>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateOrUpdateJob([FromBody] JobDto dto)
        {
            // Validate ModelState (DataAnnotations)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate DTO using the JobValidationService
            var validationResults = _jobValidationService.ValidateJob(dto).ToList();

            if (validationResults.Any())
            {
                var errors = validationResults.ToDictionary(
                    vr => string.Join(",", vr.MemberNames),
                    vr => vr.ErrorMessage
                );
                return BadRequest(new { errors, message = "Validation failed" });
            }

            // Additional security checks
            if (string.IsNullOrWhiteSpace(dto.JobId))
            {
                return BadRequest("JobId cannot be empty");
            }

            // Sanitize JobId - prevent path traversal and injection
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.JobId, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest("JobId can only contain alphanumeric characters, hyphens, and underscores");
            }

            // Validate required fields based on job type
            if (dto.Type == AutomationEngine.Models.JobType.Process && string.IsNullOrWhiteSpace(dto.Command))
                return BadRequest("Command is required for Process jobs");

            if (dto.Type == AutomationEngine.Models.JobType.PowerShell && string.IsNullOrWhiteSpace(dto.Script))
                return BadRequest("Script is required for PowerShell jobs");

            if (dto.Type == AutomationEngine.Models.JobType.FileCleanup && string.IsNullOrWhiteSpace(dto.TargetFolder))
                return BadRequest("Target Folder is required for FileCleanup jobs");

            try
            {
                // Build SaveJobRequest from JobDto
                FileCleanupOptions? fileCleanupOptions = null;
                if (dto.Type == AutomationEngine.Models.JobType.FileCleanup)
                {
                    fileCleanupOptions = new FileCleanupOptions
                    {
                        TargetFolder = dto.TargetFolder,
                        FileAgeInDays = dto.FileAgeInDays,
                        Recurse = dto.Recurse,
                        FileFilter = dto.FileFilter
                    };
                }

                var request = new SaveJobRequest
                {
                    JobId = dto.JobId,
                    DisplayName = dto.DisplayName ?? dto.JobId,
                    Type = dto.Type,
                    Command = dto.Command,
                    Arguments = dto.Arguments,
                    WorkingDirectory = dto.WorkingDirectory,
                    Script = dto.Script,
                    Schedule = dto.Schedule ?? "0 0 * * 0",
                    TimeoutSeconds = dto.TimeoutSeconds ?? 0,
                    Retry = dto.Retry,
                    OnFailureNotify = dto.OnFailureNotify,
                    Enabled = dto.Enabled,
                    FileCleanup = fileCleanupOptions
                };

                var job = await _stateManager.SaveJobAsync(request);

                return Ok(ToDto(job));
            }
            catch (ArgumentException ex)
            {
                StructuredLogger.LogValidationError(_logger, "JobEntity", "SaveJob", ex.Message);
                return BadRequest(new { error = "Invalid job data", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save job | JobId: {JobId} | Error: {Error}", 
                    dto?.JobId ?? "unknown", ex.Message);
                return BadRequest(new { error = "Failed to save job", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a job (cannot delete if running)
        /// </summary>
        [HttpDelete("{jobId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)] // Conflict - job is running
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeleteJob(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("JobId cannot be empty");
            }

            // Validate JobId format
            if (!System.Text.RegularExpressions.Regex.IsMatch(jobId, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest("Invalid JobId format");
            }

            var job = _stateManager.GetJob(jobId);
            if (job is null) 
                return NotFound(new { error = $"Job '{jobId}' not found" });

            // Prevent deletion of running jobs
            if (job.CurrentState == JobState.Running)
            {
                return Conflict(new { error = "Cannot delete a running job. Please stop it first using the stop endpoint." });
            }

            await _stateManager.DeleteJobAsync(jobId);
            return NoContent();
        }

        /// <summary>
        /// Force stop a running job
        /// </summary>
        [HttpPost("{jobId}/stop")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)] // Bad request - job not running
        public async Task<IActionResult> StopJob(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("JobId cannot be empty");
            }

            // Validate JobId format
            if (!System.Text.RegularExpressions.Regex.IsMatch(jobId, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest("Invalid JobId format");
            }

            var job = _stateManager.GetJob(jobId);
            if (job is null) 
                return NotFound(new { error = $"Job '{jobId}' not found" });

            if (job.CurrentState != JobState.Running)
            {
                return BadRequest(new { error = "Job is not currently running" });
            }

            try
            {
                // If we have a process ID, attempt to kill it
                if (job.RunningProcessId.HasValue)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById(job.RunningProcessId.Value);
                        process.Kill(true); // Kill process tree
                        StructuredLogger.LogJobStateTransition(_logger, jobId, "Running", "Stopped", 
                            $"Force-stopped via API, ProcessId: {job.RunningProcessId}");
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogWarning("Process not found for force-stop | JobId: {JobId} | ProcessId: {ProcessId} (may have already exited)", 
                            jobId, job.RunningProcessId);
                    }
                }

                // Update job state to Stopped
                await _stateManager.SetJobStateAsync(jobId, JobState.Stopped);

                return Ok(new { message = $"Job {jobId} has been stopped", jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping job | JobId: {JobId} | Error: {Error}", 
                    jobId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Enable or disable a job
        /// </summary>
        [HttpPut("{jobId}/enabled")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SetJobEnabled(string jobId, [FromBody] SetEnabledDto dto)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("JobId cannot be empty");
            }

            // Validate JobId format
            if (!System.Text.RegularExpressions.Regex.IsMatch(jobId, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest("Invalid JobId format");
            }

            // Validate DTO
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var job = _stateManager.GetJob(jobId);
            if (job is null) 
                return NotFound(new { error = $"Job '{jobId}' not found" });

            await _stateManager.SetJobEnabledAsync(jobId, dto.Enabled);
            return Ok(new { jobId, enabled = dto.Enabled });
        }

        /// <summary>
        /// Trigger a job to run immediately (validates no other job of same type is running)
        /// </summary>
        [HttpPost("{jobId}/trigger")]
        [ProducesResponseType(202)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)] // Conflict - job type already running
        [ProducesResponseType(400)]
        public async Task<IActionResult> TriggerJob(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("JobId cannot be empty");
            }

            // Validate JobId format
            if (!System.Text.RegularExpressions.Regex.IsMatch(jobId, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest("Invalid JobId format");
            }

            var job = _stateManager.GetJob(jobId);
            if (job is null) 
                return NotFound(new { error = $"Job '{jobId}' not found" });

            // Prevent multiple jobs of the same type from running simultaneously
            if (_stateManager.IsJobBusy(job.Id))
            {
                var runningJobs = _stateManager.GetRunningJobsByType(job.Type);
                return Conflict(new 
                { 
                    error = $"Cannot start job. A job of type '{job.Type}' is already running.",
                    runningJobs = runningJobs.Select(j => new { j.JobId, j.DisplayName, runningDuration = _stateManager.GetJobRunningDuration(j.JobId) }).ToList()
                });
            }

            try
            {
                // Set job state to Running before firing the task
                await _stateManager.SetJobStateAsync(jobId, JobState.Running);

                // Log the manual trigger event
                await _auditService.LogAuditEventAsync(
                    job.Id,
                    AuditLogActions.JobTriggered,
                    description: "Job manually triggered via API",
                    details: new { triggerType = "Manual", timestamp = DateTime.UtcNow }
                );

                // Broadcast job started event
                await _hubContext.BroadcastJobStartedAsync(jobId, job.DisplayName);

                // Queue job execution in background task queue for safe, observable execution
                await _backgroundTaskQueue.QueueAsync(jobId, async ct =>
                {
                    try
                    {
                        var config = new Models.JobConfig
                        {
                            Id = job.JobId,
                            JobDatabaseId = job.Id,
                            DisplayName = job.DisplayName,
                            Type = job.Type,
                            Command = job.Command,
                            Arguments = job.Arguments,
                            Script = job.Script,
                            WorkingDirectory = job.WorkingDirectory,
                            Schedule = job.Schedule,
                            TimeoutSeconds = job.TimeoutSeconds,
                            Retry = job.Retry,
                            OnFailureNotify = job.OnFailureNotify
                        };

                        var startTime = DateTime.UtcNow;
                        var result = await _runner.RunAsync(config, ct).ConfigureAwait(false);
                        var duration = DateTime.UtcNow - startTime;
                        await _stateManager.SaveJobRunAsync(job.JobId, result, startTime, duration).ConfigureAwait(false);

                        // Send notification on failure if enabled
                        if (!result.Success && job.OnFailureNotify)
                        {
                            try
                            {
                                await _ntfyService.SendFailureNotificationAsync(
                                    job.DisplayName,
                                    result.StdErr ?? "No error details available",
                                    ct).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send ntfy notification for job {JobId}", jobId);
                            }
                        }

                        // Broadcast job completed event
                        await _hubContext.BroadcastJobCompletedAsync(jobId, result.Success, result.ExitCode, result.StdErr, (int)duration.TotalMilliseconds).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing triggered job {JobId}", jobId);
                        // Broadcast error state
                        await _hubContext.BroadcastJobCompletedAsync(jobId, false, -1, ex.Message).ConfigureAwait(false);
                    }
                    finally
                    {
                        // Notify all clients that jobs list has been updated
                        await _hubContext.BroadcastJobsListUpdatedAsync().ConfigureAwait(false);
                    }
                });

                return Accepted(new { message = $"Job {jobId} triggered and running", jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering job {JobId}", jobId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get job run history
        /// </summary>
        [HttpGet("{jobId}/history")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetJobHistory(string jobId, [FromQuery] int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("JobId cannot be empty");
            }

            // Validate JobId format
            if (!System.Text.RegularExpressions.Regex.IsMatch(jobId, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest("Invalid JobId format");
            }

            // Validate limit parameter
            if (limit < 1 || limit > 1000)
            {
                return BadRequest("Limit must be between 1 and 1000");
            }

            var job = _stateManager.GetJob(jobId);
            if (job is null) 
                return NotFound(new { error = $"Job '{jobId}' not found" });

            var runs = await _stateManager.GetJobRunsAsync(jobId, limit);
            var dto = runs.Select(r => new JobRunDto
            {
                Id = r.Id,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                Success = r.Success,
                ExitCode = r.ExitCode,
                StdOut = r.StdOut?.Substring(0, Math.Min(500, r.StdOut.Length)) ?? "",
                StdErr = r.StdErr?.Substring(0, Math.Min(500, r.StdErr.Length)) ?? "",
                DurationMs = r.DurationMs
            }).ToList();

            return Ok(dto);
        }

        private JobDto ToDto(JobEntity job)
        {
            var lastRun = job.Runs
                .Where(r => r.CompletedAt.HasValue)
                .OrderByDescending(r => r.CompletedAt)
                .FirstOrDefault();
            return new JobDto
            {
                JobId = job.JobId,
                DisplayName = job.DisplayName,
                Type = job.Type,
                Command = job.Command,
                Arguments = job.Arguments,
                Script = job.Script,
                WorkingDirectory = job.WorkingDirectory,
                Schedule = job.Schedule,
                TimeoutSeconds = job.TimeoutSeconds,
                Retry = job.Retry,
                OnFailureNotify = job.OnFailureNotify,
                Enabled = job.Enabled,
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt,
                LastRun = lastRun?.CompletedAt,
                LastRunSuccess = lastRun?.Success,
                IsRunning = job.CurrentState == JobState.Running,
                CurrentState = job.CurrentState,
                RunningStartTime = job.RunningStartTime,
                RunningProcessId = job.RunningProcessId,
                TargetFolder = job.TargetFolder,
                FileAgeInDays = job.FileAgeInDays,
                Recurse = job.Recurse,
                FileFilter = job.FileFilter
            };
        }
    }

}
