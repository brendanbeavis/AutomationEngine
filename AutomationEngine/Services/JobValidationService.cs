using System.ComponentModel.DataAnnotations;
using Cronos;
using AutomationEngine.Configuration;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Services.Abstractions;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Service implementation for validating job data transfer objects.
    /// </summary>
    /// <remarks>
    /// Extracted from JobDto to maintain separation of concerns and keep DTOs lightweight.
    /// Handles all business-level validation for job configurations.
    /// </remarks>
    public class JobValidationService : IJobValidationService
    {
        /// <summary>
        /// Validates a job DTO and returns any validation errors.
        /// </summary>
        /// <param name="job">The job DTO to validate.</param>
        /// <returns>An enumerable collection of validation errors, if any.</returns>
        public IEnumerable<ValidationResult> ValidateJob(JobDto job)
        {
            if (job == null)
            {
                yield return new ValidationResult("Job cannot be null");
                yield break;
            }

            // Type-specific validations
            if (job.Type == JobType.Process)
            {
                if (string.IsNullOrWhiteSpace(job.Command))
                {
                    yield return new ValidationResult("Command is required for Process jobs", new[] { nameof(job.Command) });
                }
                else if (job.Command.Length > 500)
                {
                    yield return new ValidationResult("Command exceeds maximum length of 500 characters", new[] { nameof(job.Command) });
                }
            }

            if (job.Type == JobType.PowerShell)
            {
                if (string.IsNullOrWhiteSpace(job.Script))
                {
                    yield return new ValidationResult("Script is required for PowerShell jobs", new[] { nameof(job.Script) });
                }
                else if (job.Script.Length > Constants.Jobs.CommandMaxLength)
                {
                    yield return new ValidationResult($"Script exceeds maximum length of {Constants.Jobs.CommandMaxLength} characters", new[] { nameof(job.Script) });
                }
            }

            if (job.Type == JobType.FileCleanup)
            {
                if (string.IsNullOrWhiteSpace(job.TargetFolder))
                {
                    yield return new ValidationResult("Target Folder is required for FileCleanup jobs", new[] { nameof(job.TargetFolder) });
                }
                else if (!IsValidFilePath(job.TargetFolder))
                {
                    yield return new ValidationResult("Target Folder contains invalid path characters", new[] { nameof(job.TargetFolder) });
                }
            }

            // Schedule validation
            if (!string.IsNullOrWhiteSpace(job.Schedule))
            {
                if (!IsValidCronSchedule(job.Schedule, out string? cronError))
                {
                    yield return new ValidationResult($"Invalid cron schedule expression: {cronError}", new[] { nameof(job.Schedule) });
                }
            }

            // Working directory validation
            if (!string.IsNullOrWhiteSpace(job.WorkingDirectory) && !IsValidFilePath(job.WorkingDirectory))
            {
                yield return new ValidationResult("WorkingDirectory contains invalid path characters", new[] { nameof(job.WorkingDirectory) });
            }

            // Arguments length check
            if (!string.IsNullOrWhiteSpace(job.Arguments) && job.Arguments.Length > 500)
            {
                yield return new ValidationResult("Arguments exceed maximum length of 500 characters", new[] { nameof(job.Arguments) });
            }
        }

        /// <summary>
        /// Validates whether a file path contains only valid characters.
        /// </summary>
        /// <param name="path">The file path to validate.</param>
        /// <returns>True if the path is valid or empty; false if it contains invalid path characters.</returns>
        public bool IsValidFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            // Check for invalid path characters
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }

        /// <summary>
        /// Validates a CRON schedule expression.
        /// </summary>
        /// <param name="schedule">The CRON expression to validate.</param>
        /// <param name="errorMessage">Output parameter containing the error message if validation fails.</param>
        /// <returns>True if the schedule is valid; false otherwise.</returns>
        /// <remarks>
        /// Attempts validation in order:
        /// 1. Extended format (6 fields with seconds)
        /// 2. Standard format (5 fields)
        /// If both fail, returns the error from the standard format attempt.
        /// </remarks>
        public bool IsValidCronSchedule(string? schedule, out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(schedule))
                return true;

            bool parsed = false;
            string? cronError = null;

            try
            {
                CronExpression.Parse(schedule, CronFormat.IncludeSeconds);
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
                    CronExpression.Parse(schedule, CronFormat.Standard);
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
                errorMessage = cronError;
                return false;
            }

            return true;
        }
    }
}
