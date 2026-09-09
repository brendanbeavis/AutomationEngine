using System.ComponentModel.DataAnnotations;
using AutomationEngine.Application.Validation;
using AutomationEngine.Models;

namespace AutomationEngine.Api.Requests
{
    /// <summary>
    /// Parameter object for SaveJobAsync operation.
    /// Encapsulates all job save parameters with type-safe structure.
    /// </summary>
    public class SaveJobRequest : IValidatableObject
    {
        /// <summary>
        /// Unique identifier for the job
        /// </summary>
        [Required(ErrorMessage = "JobId is required")]
        [StringLength(JobValidationRules.JobIdMaxLength, MinimumLength = JobValidationRules.JobIdMinLength, ErrorMessage = "JobId must be between 1 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "JobId can only contain alphanumeric characters, hyphens, and underscores")]
        public required string JobId { get; set; }

        /// <summary>
        /// Display name for the job
        /// </summary>
        [Required(ErrorMessage = "DisplayName is required")]
        [StringLength(JobValidationRules.DisplayNameMaxLength, MinimumLength = JobValidationRules.DisplayNameMinLength, ErrorMessage = "DisplayName must be between 1 and 200 characters")]
        public required string DisplayName { get; set; }

        /// <summary>
        /// Job type (Process, PowerShell, FileCleanup, etc.)
        /// </summary>
        [Required(ErrorMessage = "Type is required")]
        public required JobType Type { get; set; }

        /// <summary>
        /// Command to execute for Process jobs
        /// </summary>
        [StringLength(JobValidationRules.CommandMaxLength)]
        public string? Command { get; set; }

        /// <summary>
        /// Arguments to pass to the command
        /// </summary>
        [StringLength(JobValidationRules.ArgumentsMaxLength)]
        public string? Arguments { get; set; }

        /// <summary>
        /// Working directory for job execution
        /// </summary>
        [StringLength(JobValidationRules.WorkingDirectoryMaxLength)]
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// PowerShell script content
        /// </summary>
        public string? Script { get; set; }

        /// <summary>
        /// CRON schedule expression
        /// </summary>
        [StringLength(JobValidationRules.ScheduleMaxLength)]
        public string? Schedule { get; set; }

        /// <summary>
        /// Timeout for job execution in seconds
        /// </summary>
        [Range(JobValidationRules.TimeoutMinSeconds, JobValidationRules.TimeoutMaxSeconds, ErrorMessage = "TimeoutSeconds must be between 0 and 86400")]
        public int TimeoutSeconds { get; set; } = 0;

        /// <summary>
        /// Number of retries on failure
        /// </summary>
        [Range(JobValidationRules.RetryMin, JobValidationRules.RetryMax, ErrorMessage = "Retry must be between 0 and 100")]
        public int Retry { get; set; } = 0;

        /// <summary>
        /// Whether to send notifications on failure
        /// </summary>
        public bool OnFailureNotify { get; set; } = false;

        /// <summary>
        /// Whether to send notifications on success
        /// </summary>
        public bool OnSuccessNotify { get; set; } = false;

        /// <summary>
        /// Whether the job is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// File cleanup specific options (only applicable for FileCleanup job type)
        /// </summary>
        public FileCleanupOptions? FileCleanup { get; set; }

        /// <summary>
        /// Validate the request based on job type
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            // Validate required fields based on job type
            if (Type == JobType.Process && string.IsNullOrWhiteSpace(Command))
            {
                errors.Add(new ValidationResult(
                    "Command is required for Process jobs",
                    new[] { nameof(Command) }
                ));
            }

            if (Type == JobType.PowerShell && string.IsNullOrWhiteSpace(Script))
            {
                errors.Add(new ValidationResult(
                    "Script is required for PowerShell jobs",
                    new[] { nameof(Script) }
                ));
            }

            if (Type == JobType.FileCleanup)
            {
                if (FileCleanup == null)
                {
                    errors.Add(new ValidationResult(
                        "FileCleanup options are required for FileCleanup jobs",
                        new[] { nameof(FileCleanup) }
                    ));
                }
                else if (string.IsNullOrWhiteSpace(FileCleanup.TargetFolder))
                {
                    errors.Add(new ValidationResult(
                        "TargetFolder is required for FileCleanup jobs",
                        new[] { nameof(FileCleanup) }
                    ));
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Nested object for file cleanup specific configuration
    /// </summary>
    public class FileCleanupOptions
    {
        /// <summary>
        /// Target folder for file cleanup
        /// </summary>
        [StringLength(JobValidationRules.TargetFolderMaxLength, MinimumLength = 1, ErrorMessage = "TargetFolder must be between 1 and 500 characters")]
        public string? TargetFolder { get; set; }

        /// <summary>
        /// Age of files in days (delete files older than this)
        /// </summary>
        [Range(JobValidationRules.FileAgeMinDays, JobValidationRules.FileAgeMaxDays, ErrorMessage = "FileAgeInDays must be between 0 and 36500")]
        public int FileAgeInDays { get; set; } = 30;

        /// <summary>
        /// Whether to recursively search subdirectories
        /// </summary>
        public bool Recurse { get; set; } = false;

        /// <summary>
        /// File filter pattern (e.g., "*.log")
        /// </summary>
        [StringLength(JobValidationRules.FileFilterMaxLength)]
        public string? FileFilter { get; set; }
    }
}
