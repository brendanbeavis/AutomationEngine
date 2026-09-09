using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutomationEngine.Application.Validation;
using AutomationEngine.Configuration;
using AutomationEngine.Models;

namespace AutomationEngine.Dto
{
    /// <summary>
    /// Data Transfer Object representing a job configuration with execution details.
    /// </summary>
    /// <remarks>
    /// Contains comprehensive job configuration including scheduling, execution parameters, and runtime state.
    /// Supports multiple job types (Process, PowerShell, FileCleanup).
    /// 
    /// This is a pure data transfer object with DataAnnotation-based validation attributes.
    /// Business logic for validation and status reporting has been extracted to dedicated services:
    /// - IJobValidationService: Handles complex validation rules
    /// - IJobStatusService: Handles status reporting and scheduling information
    /// </remarks>
    public class JobDto : IValidatableObject
    {
        /// <summary>
        /// Unique identifier for the job (alphanumeric with hyphens and underscores allowed).
        /// </summary>
        [Required(ErrorMessage = "JobId is required")]
        [StringLength(JobValidationRules.JobIdMaxLength, MinimumLength = JobValidationRules.JobIdMinLength, ErrorMessage = "JobId must be between 1 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "JobId can only contain alphanumeric characters, hyphens, and underscores")]
        public string? JobId { get; set; }

        /// <summary>
        /// Human-readable display name for the job.
        /// </summary>
        [Required(ErrorMessage = "DisplayName is required")]
        [StringLength(JobValidationRules.DisplayNameMaxLength, MinimumLength = JobValidationRules.DisplayNameMinLength, ErrorMessage = "DisplayName must be between 1 and 200 characters")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Type of job to execute (Process, PowerShell, FileCleanup).
        /// </summary>
        [Required(ErrorMessage = "Job type is required")]
        public JobType Type { get; set; } = JobType.Process;

        /// <summary>
        /// Command to execute for Process jobs (e.g., application path or executable name).
        /// </summary>
        [StringLength(JobValidationRules.CommandMaxLength, ErrorMessage = "Command is limited to 500 characters")]
        public string? Command { get; set; }

        /// <summary>
        /// Arguments to pass to the command for Process jobs.
        /// </summary>
        [StringLength(JobValidationRules.ArgumentsMaxLength, ErrorMessage = "Arguments are limited to 500 characters")]
        public string? Arguments { get; set; }

        /// <summary>
        /// PowerShell script content for PowerShell jobs.
        /// </summary>
        [StringLength(Constants.Jobs.CommandMaxLength, ErrorMessage = "Script is limited to character length of " + nameof(Constants.Jobs.CommandMaxLength))]
        public string? Script { get; set; }

        /// <summary>
        /// Working directory for job execution.
        /// </summary>
        [StringLength(JobValidationRules.WorkingDirectoryMaxLength, ErrorMessage = "WorkingDirectory is limited to 200 characters")]
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// CRON schedule expression for job execution (supports both standard and 6-field formats).
        /// </summary>
        [StringLength(JobValidationRules.ScheduleMaxLength, ErrorMessage = "Schedule is limited to 50 characters")]
        public string? Schedule { get; set; }

        /// <summary>
        /// Maximum execution time in seconds for the job (0-86400 or 24 hours).
        /// </summary>
        [Range(JobValidationRules.TimeoutMinSeconds, JobValidationRules.TimeoutMaxSeconds, ErrorMessage = "TimeoutSeconds must be between 0 and 86400 (24 hours)")]
        public int? TimeoutSeconds { get; set; }

        /// <summary>
        /// Number of retry attempts on job failure (0-100).
        /// </summary>
        [Range(JobValidationRules.RetryMin, JobValidationRules.RetryMax, ErrorMessage = "Retry count must be between 0 and 100")]
        public int Retry { get; set; } = 0;

        /// <summary>
        /// Indicates whether to send notifications when the job fails.
        /// </summary>
        public bool OnFailureNotify { get; set; } = false;

        /// <summary>
        /// Indicates whether to send notifications when the job succeeds.
        /// </summary>
        public bool OnSuccessNotify { get; set; } = false;

        /// <summary>
        /// Indicates whether the job is enabled and eligible for execution.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Timestamp when the job was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the job was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Timestamp of the job's last execution.
        /// </summary>
        public DateTime? LastRun { get; set; }

        /// <summary>
        /// Indicates whether the last job execution was successful.
        /// </summary>
        public bool? LastRunSuccess { get; set; }

        /// <summary>
        /// Legacy property indicating if the job is currently running.
        /// </summary>
        public bool IsRunning { get; set; } = false;

        /// <summary>
        /// Current state of the job (Idle, Queued, Running, Completed).
        /// </summary>
        public JobState CurrentState { get; set; } = JobState.Idle;

        /// <summary>
        /// Timestamp when the current job run started.
        /// </summary>
        public DateTime? RunningStartTime { get; set; }

        /// <summary>
        /// Process ID of the currently running job instance.
        /// </summary>
        public int? RunningProcessId { get; set; }

        /// <summary>
        /// Target folder for file cleanup operations (FileCleanup job type only).
        /// </summary>
        [StringLength(JobValidationRules.TargetFolderMaxLength, ErrorMessage = "TargetFolder is limited to 500 characters")]
        public string? TargetFolder { get; set; }

        /// <summary>
        /// Number of days to use as the threshold for file cleanup (delete files older than this value).
        /// </summary>
        [Range(JobValidationRules.FileAgeMinDays, JobValidationRules.FileAgeMaxDays, ErrorMessage = "FileAgeInDays must be between 0 and 36500 (100 years)")]
        public int FileAgeInDays { get; set; } = 0;

        /// <summary>
        /// Indicates whether to recursively search subdirectories during file cleanup.
        /// </summary>
        public bool Recurse { get; set; } = false;

        /// <summary>
        /// File filter pattern for cleanup operations (e.g., "*.log", "*.tmp"). Uses wildcard notation.
        /// </summary>
        [StringLength(JobValidationRules.FileFilterMaxLength, ErrorMessage = "FileFilter is limited to 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_.*?\-\[\]]+$", ErrorMessage = "FileFilter contains invalid characters")]
        public string? FileFilter { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type == JobType.Process && string.IsNullOrWhiteSpace(Command))
            {
                yield return new ValidationResult("Command is required for Process jobs", new[] { nameof(Command) });
            }

            if (Type == JobType.PowerShell && string.IsNullOrWhiteSpace(Script))
            {
                yield return new ValidationResult("Script is required for PowerShell jobs", new[] { nameof(Script) });
            }

            if (Type == JobType.FileCleanup && string.IsNullOrWhiteSpace(TargetFolder))
            {
                yield return new ValidationResult("TargetFolder is required for FileCleanup jobs", new[] { nameof(TargetFolder) });
            }
        }
    }
}
