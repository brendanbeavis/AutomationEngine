using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomationEngine.Data.Entities
{
    [Table("AppSettings")]
    public class AppSettingsEntity
    {
        [Key]
        public int Id { get; set; } = 1; // Singleton table - only one record

        /// <summary>
        /// Current application theme: "cyberpunk", "dark", or "light"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Theme { get; set; } = "cyberpunk";

        /// <summary>
        /// Logging level: "Trace", "Debug", "Information", "Warning", "Error", "Critical"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string LoggingLevel { get; set; } = "Information";

        /// <summary>
        /// Enable or disable job execution
        /// </summary>
        public bool JobExecutionEnabled { get; set; } = true;

        /// <summary>
        /// Default timeout for jobs in seconds (0 = no timeout)
        /// </summary>
        public int DefaultJobTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Ntfy endpoint URL for push notifications
        /// </summary>
        [MaxLength(512)]
        public string? NtfyEndpoint { get; set; }

        /// <summary>
        /// Enable detailed logging for all job runs
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Maximum number of job history records to retain per job
        /// </summary>
        public int MaxJobHistoryRecords { get; set; } = 1000;

        /// <summary>
        /// Application name/title
        /// </summary>
        [MaxLength(255)]
        public string ApplicationName { get; set; } = "Automation Engine";

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
