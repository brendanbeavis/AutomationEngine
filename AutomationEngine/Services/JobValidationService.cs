using System.ComponentModel.DataAnnotations;
using Cronos;
using AutomationEngine.Configuration;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<JobValidationService> _logger;

        public JobValidationService(ILogger<JobValidationService> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Validates a job DTO and returns any validation errors.
        /// </summary>
        /// <param name="job">The job DTO to validate.</param>
        /// <returns>An enumerable collection of validation errors, if any.</returns>
        public IEnumerable<ValidationResult> ValidateJob(JobDto job)
        {
            if (job == null)
            {
                _logger.LogDebug("Job validation failed: job is null");
                yield return new ValidationResult("Job cannot be null");
                yield break;
            }

            _logger.LogDebug("Validating job | JobId: {JobId} | Type: {JobType}", job.JobId, job.Type);
            var errors = new List<ValidationResult>();

            // Type-specific validations
            if (job.Type == JobType.Process)
            {
                if (string.IsNullOrWhiteSpace(job.Command))
                {
                    var error = new ValidationResult("Command is required for Process jobs", new[] { nameof(job.Command) });
                    errors.Add(error);
                    _logger.LogWarning("Job validation failed: Command is required | JobId: {JobId}", job.JobId);
                }
                else if (job.Command.Length > 500)
                {
                    var error = new ValidationResult("Command exceeds maximum length of 500 characters", new[] { nameof(job.Command) });
                    errors.Add(error);
                    _logger.LogWarning("Job validation failed: Command too long | JobId: {JobId}", job.JobId);
                }
            }

            if (job.Type == JobType.PowerShell)
            {
                if (string.IsNullOrWhiteSpace(job.Script))
                {
                    var error = new ValidationResult("Script is required for PowerShell jobs", new[] { nameof(job.Script) });
                    errors.Add(error);
                    _logger.LogWarning("Job validation failed: Script is required | JobId: {JobId}", job.JobId);
                }
                else if (job.Script.Length > Constants.Jobs.CommandMaxLength)
                {
                    var error = new ValidationResult($"Script exceeds maximum length of {Constants.Jobs.CommandMaxLength} characters", new[] { nameof(job.Script) });
                    errors.Add(error);
                    _logger.LogWarning("Job validation failed: Script too long | JobId: {JobId}", job.JobId);
                }
            }

            if (job.Type == JobType.FileCleanup)
            {
                if (string.IsNullOrWhiteSpace(job.TargetFolder))
                {
                    var error = new ValidationResult("Target Folder is required for FileCleanup jobs", new[] { nameof(job.TargetFolder) });
                    errors.Add(error);
                    _logger.LogWarning("Job validation failed: TargetFolder is required | JobId: {JobId}", job.JobId);
                }
                else if (!IsValidFilePath(job.TargetFolder))
                {
                    var error = new ValidationResult("Target Folder contains invalid path characters", new[] { nameof(job.TargetFolder) });
                    errors.Add(error);
                    _logger.LogWarning("Job validation failed: Invalid path | JobId: {JobId} | Path: {Path}", job.JobId, job.TargetFolder);
                }
            }

            // Schedule validation
            if (!string.IsNullOrWhiteSpace(job.Schedule))
            {
                if (!IsValidCronSchedule(job.Schedule, out string? cronError))
                {
                    var error = new ValidationResult($"Invalid cron schedule expression: {cronError}", new[] { nameof(job.Schedule) });
                    errors.Add(error);
                    _logger.LogWarning("Job validation failed: Invalid cron schedule | JobId: {JobId} | Schedule: {Schedule} | Error: {Error}", 
                        job.JobId, job.Schedule, cronError);
                }
            }

            // Working directory validation
            if (!string.IsNullOrWhiteSpace(job.WorkingDirectory) && !IsValidFilePath(job.WorkingDirectory))
            {
                var error = new ValidationResult("WorkingDirectory contains invalid path characters", new[] { nameof(job.WorkingDirectory) });
                errors.Add(error);
                _logger.LogWarning("Job validation failed: Invalid working directory | JobId: {JobId}", job.JobId);
            }

            // Arguments length check
            if (!string.IsNullOrWhiteSpace(job.Arguments) && job.Arguments.Length > 500)
            {
                var error = new ValidationResult("Arguments exceed maximum length of 500 characters", new[] { nameof(job.Arguments) });
                errors.Add(error);
                _logger.LogWarning("Job validation failed: Arguments too long | JobId: {JobId}", job.JobId);
            }

            if (errors.Count == 0)
            {
                _logger.LogDebug("Job validation passed | JobId: {JobId}", job.JobId);
            }
            else
            {
                _logger.LogWarning("Job validation completed with {ErrorCount} errors | JobId: {JobId}", errors.Count, job.JobId);
            }

            foreach (var error in errors)
            {
                yield return error;
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
