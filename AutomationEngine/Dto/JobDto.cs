using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cronos;
using AutomationEngine.Configuration;
using AutomationEngine.Models;

namespace AutomationEngine.Dto
{
    public class JobDto : IValidatableObject
    {
        [Required(ErrorMessage = "JobId is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "JobId must be between 1 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "JobId can only contain alphanumeric characters, hyphens, and underscores")]
        public string? JobId { get; set; }

        [Required(ErrorMessage = "DisplayName is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "DisplayName must be between 1 and 200 characters")]
        public string? DisplayName { get; set; }

        [Required(ErrorMessage = "Job type is required")]
        public JobType Type { get; set; } = JobType.Process;

        [StringLength(500, ErrorMessage = "Command is limited to 500 characters")]
        public string? Command { get; set; }

        [StringLength(500, ErrorMessage = "Arguments are limited to 500 characters")]
        public string? Arguments { get; set; }

        [StringLength(Constants.Jobs.CommandMaxLength, ErrorMessage = "Script is limited to character length of " + nameof(Constants.Jobs.CommandMaxLength))]
        public string? Script { get; set; }

        [StringLength(260, ErrorMessage = "WorkingDirectory is limited to 260 characters")]
        public string? WorkingDirectory { get; set; }

        [StringLength(100, ErrorMessage = "Schedule is limited to 100 characters")]
        public string? Schedule { get; set; }

        [Range(0, 86400, ErrorMessage = "TimeoutSeconds must be between 0 and 86400 (24 hours)")]
        public int? TimeoutSeconds { get; set; }

        [Range(0, 100, ErrorMessage = "Retry count must be between 0 and 100")]
        public int Retry { get; set; } = 0;

        public bool OnFailureNotify { get; set; } = false;
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastRun { get; set; }
        public bool? LastRunSuccess { get; set; }
        public bool IsRunning { get; set; } = false;

        // Job state tracking
        public JobState CurrentState { get; set; } = JobState.Idle;
        public DateTime? RunningStartTime { get; set; }
        public int? RunningProcessId { get; set; }

        // FileCleanup job specific properties
        [StringLength(260, ErrorMessage = "TargetFolder is limited to 260 characters")]
        public string? TargetFolder { get; set; }

        [Range(0, 36500, ErrorMessage = "FileAgeInDays must be between 0 and 36500 (100 years)")]
        public int FileAgeInDays { get; set; } = 0;

        public bool Recurse { get; set; } = false;

        [StringLength(100, ErrorMessage = "FileFilter is limited to 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_.*?\-\[\]]+$", ErrorMessage = "FileFilter contains invalid characters")]
        public string? FileFilter { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Type-specific validations
            if (Type == JobType.Process)
            {
                if (string.IsNullOrWhiteSpace(Command))
                {
                    yield return new ValidationResult("Command is required for Process jobs", new[] { nameof(Command) });
                }
                else if (Command.Length > 500)
                {
                    yield return new ValidationResult("Command exceeds maximum length of 500 characters", new[] { nameof(Command) });
                }
            }

            if (Type == JobType.PowerShell)
            {
                if (string.IsNullOrWhiteSpace(Script))
                {
                    yield return new ValidationResult("Script is required for PowerShell jobs", new[] { nameof(Script) });
                }
                else if (Script.Length > Constants.Jobs.CommandMaxLength)
                {
                    yield return new ValidationResult($"Script exceeds maximum length of {Constants.Jobs.CommandMaxLength} characters", new[] { nameof(Script) });
                }
            }

            if (Type == JobType.FileCleanup)
            {
                if (string.IsNullOrWhiteSpace(TargetFolder))
                {
                    yield return new ValidationResult("Target Folder is required for FileCleanup jobs", new[] { nameof(TargetFolder) });
                }
                else if (!IsValidFilePath(TargetFolder))
                {
                    yield return new ValidationResult("Target Folder contains invalid path characters", new[] { nameof(TargetFolder) });
                }
            }

            // Schedule validation
            if (!string.IsNullOrWhiteSpace(Schedule))
            {
                bool parsed = false;
                string? cronError = null;

                try
                {
                    CronExpression.Parse(Schedule, CronFormat.IncludeSeconds);
                    parsed = true;
                }
                catch (CronFormatException ex)
                {
                    // First format (IncludeSeconds) failed, will try standard format next
                    cronError = ex.Message;
                }

                if (!parsed)
                {
                    try
                    {
                        CronExpression.Parse(Schedule, CronFormat.Standard);
                        parsed = true;
                    }
                    catch (CronFormatException ex)
                    {
                        // Both formats failed - use the error from standard format as it's more general
                        cronError = ex.Message;
                    }
                }

                if (!parsed)
                {
                    yield return new ValidationResult($"Invalid cron schedule expression: {cronError}", new[] { nameof(Schedule) });
                }
            }

            // Working directory validation
            if (!string.IsNullOrWhiteSpace(WorkingDirectory) && !IsValidFilePath(WorkingDirectory))
            {
                yield return new ValidationResult("WorkingDirectory contains invalid path characters", new[] { nameof(WorkingDirectory) });
            }

            // Arguments length check
            if (!string.IsNullOrWhiteSpace(Arguments) && Arguments.Length > 500)
            {
                yield return new ValidationResult("Arguments exceed maximum length of 500 characters", new[] { nameof(Arguments) });
            }
        }

        private bool IsValidFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            // Check for invalid path characters
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }

        public string JobStatus()
        {
            CronExpression expression = CronExpression.Parse(Schedule);

            // Calculate next run matching that specific zone
            DateTime? nextUtc = expression.GetNextOccurrence(DateTime.UtcNow);
            string response = string.Empty;

            if (IsRunning)
            {
                response += "Job is currently running; ";
            }

            if (nextUtc.HasValue)
            {
                response += "Next run:" + nextUtc.Value.ToLocalTime().ToString() + "; ";
            }

            if (!Enabled)
            {
                response += "However job schedule is disabled!; ";
            }

            return response;
        }
    }
}
