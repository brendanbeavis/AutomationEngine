using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomationEngine.Data.Entities
{
    /// <summary>
    /// Audit log entry tracking job lifecycle and execution events.
    /// Each significant job activity creates an audit log entry for compliance and debugging.
    /// </summary>
    [Table("AuditLogs")]
    public class AuditLogEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public JobEntity Job { get; set; } = null!;

        /// <summary>
        /// Type of activity: Created, Updated, Deleted, Enabled, Disabled, Started, Completed, Failed, Stopped, etc.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the activity occurred
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Detailed description of the activity
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// For job runs, store the run result (Success/Failure)
        /// </summary>
        public bool? Success { get; set; }

        /// <summary>
        /// For job runs, store the exit code
        /// </summary>
        public int? ExitCode { get; set; }

        /// <summary>
        /// Duration in milliseconds for execution events
        /// </summary>
        public int? DurationMs { get; set; }

        /// <summary>
        /// Error message if applicable
        /// </summary>
        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Free-form details JSON for additional context
        /// </summary>
        [MaxLength(2000)]
        public string? Details { get; set; }
    }

    /// <summary>
    /// Audit log action types
    /// </summary>
    public static class AuditLogActions
    {
        // Job lifecycle
        public const string JobCreated = "JobCreated";
        public const string JobUpdated = "JobUpdated";
        public const string JobDeleted = "JobDeleted";
        public const string JobEnabled = "JobEnabled";
        public const string JobDisabled = "JobDisabled";

        // Job execution
        public const string JobStarted = "JobStarted";
        public const string JobCompleted = "JobCompleted";
        public const string JobFailed = "JobFailed";
        public const string JobStopped = "JobStopped";
        public const string JobRetrying = "JobRetrying";

        // Manual triggers
        public const string JobTriggered = "JobTriggered";
    }
}
