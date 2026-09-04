using System;
using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Dto
{
    /// <summary>
    /// Data Transfer Object for application-wide settings and configuration.
    /// </summary>
    /// <remarks>
    /// Contains global application settings including theme preferences, logging configuration, 
    /// job execution defaults, and notification settings. This DTO is used to manage and persist 
    /// application-level preferences.
    /// </remarks>
    public class AppSettingsDto
    {
        /// <summary>
        /// Unique identifier for the application settings record.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive number")]
        public int Id { get; set; }

        /// <summary>
        /// Current UI theme (e.g., "cyberpunk", "default").
        /// </summary>
        [Required(ErrorMessage = "Theme is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Theme must be between 1 and 50 characters")]
        public string Theme { get; set; } = "cyberpunk";

        /// <summary>
        /// Logging level for the application (e.g., "Information", "Warning", "Error").
        /// </summary>
        [Required(ErrorMessage = "LoggingLevel is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "LoggingLevel must be between 1 and 50 characters")]
        public string LoggingLevel { get; set; } = "Information";

        /// <summary>
        /// Indicates whether job execution is enabled globally.
        /// </summary>
        public bool JobExecutionEnabled { get; set; } = true;

        /// <summary>
        /// Default timeout in seconds for job execution.
        /// </summary>
        [Range(1, 86400, ErrorMessage = "DefaultJobTimeoutSeconds must be between 1 and 86400 seconds (24 hours)")]
        public int DefaultJobTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Indicates whether email notifications are enabled.
        /// </summary>
        public bool EnableEmailNotifications { get; set; } = false;

        /// <summary>
        /// Email address for receiving notifications.
        /// </summary>
        [EmailAddress(ErrorMessage = "NotificationEmail must be a valid email address")]
        public string? NotificationEmail { get; set; }

        /// <summary>
        /// Ntfy service endpoint URL for push notifications.
        /// </summary>
        [Url(ErrorMessage = "NtfyEndpoint must be a valid URL")]
        public string? NtfyEndpoint { get; set; }

        /// <summary>
        /// Indicates whether detailed logging is enabled.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Maximum number of job history records to retain.
        /// </summary>
        [Range(100, 1000000, ErrorMessage = "MaxJobHistoryRecords must be between 100 and 1,000,000")]
        public int MaxJobHistoryRecords { get; set; } = 1000;

        /// <summary>
        /// Display name of the application.
        /// </summary>
        [Required(ErrorMessage = "ApplicationName is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "ApplicationName must be between 1 and 200 characters")]
        public string ApplicationName { get; set; } = "Automation Engine";

        /// <summary>
        /// Timestamp when the settings were last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Timestamp when the settings record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
