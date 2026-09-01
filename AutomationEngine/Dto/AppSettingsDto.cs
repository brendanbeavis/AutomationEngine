using System;

namespace AutomationEngine.Dto
{
    public class AppSettingsDto
    {
        public int Id { get; set; }

        public string Theme { get; set; } = "cyberpunk";

        public string LoggingLevel { get; set; } = "Information";

        public bool JobExecutionEnabled { get; set; } = true;

        public int DefaultJobTimeoutSeconds { get; set; } = 300;

        public bool EnableEmailNotifications { get; set; } = false;

        public string? NotificationEmail { get; set; }

        public string? NtfyEndpoint { get; set; }

        public bool EnableDetailedLogging { get; set; } = false;

        public int MaxJobHistoryRecords { get; set; } = 1000;

        public string ApplicationName { get; set; } = "Automation Engine";

        public DateTime UpdatedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
