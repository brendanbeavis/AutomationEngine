using System.ComponentModel.DataAnnotations;
using AutomationEngine.Dto;

namespace AutomationEngine.Services.Abstractions
{
    /// <summary>
    /// Service for validating job data transfer objects.
    /// Single Responsibility: Validate job configurations and data integrity
    /// </summary>
    /// <remarks>
    /// Handles all business-level validation for jobs including:
    /// - Type-specific field validation (Command for Process, Script for PowerShell, etc.)
    /// - CRON schedule expression validation
    /// - File path validation
    /// - String length and format constraints
    /// 
    /// This service extracts validation logic from the DTO to maintain separation of concerns.
    /// </remarks>
    public interface IJobValidationService
    {
        /// <summary>
        /// Validates a job DTO and returns any validation errors.
        /// </summary>
        /// <param name="job">The job DTO to validate.</param>
        /// <returns>An enumerable collection of validation errors, if any.</returns>
        /// <remarks>
        /// Performs comprehensive validation including type-specific requirements,
        /// CRON expression syntax, and file path validity.
        /// </remarks>
        IEnumerable<ValidationResult> ValidateJob(JobDto job);

        /// <summary>
        /// Validates whether a file path contains only valid characters.
        /// </summary>
        /// <param name="path">The file path to validate.</param>
        /// <returns>True if the path is valid or empty; false if it contains invalid path characters.</returns>
        bool IsValidFilePath(string path);

        /// <summary>
        /// Validates a CRON schedule expression.
        /// </summary>
        /// <param name="schedule">The CRON expression to validate.</param>
        /// <param name="errorMessage">Output parameter containing the error message if validation fails.</param>
        /// <returns>True if the schedule is valid; false otherwise.</returns>
        /// <remarks>
        /// Supports both standard (5-field) and extended (6-field with seconds) CRON formats.
        /// </remarks>
        bool IsValidCronSchedule(string? schedule, out string? errorMessage);
    }
}
