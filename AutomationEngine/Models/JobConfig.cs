using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutomationEngine.Models
{
    public class JobConfig
    {
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Database ID for the job (used for audit logging)
        /// </summary>
        public int? JobDatabaseId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public JobType Type { get; set; } = JobType.Process;
        public string Command { get; set; } = string.Empty;
        // For process jobs - command line arguments. For PowerShell jobs this is ignored.
        public string? Arguments { get; set; }
        // For PowerShell jobs - the script content to execute.
        public string? Script { get; set; }
        public string? WorkingDirectory { get; set; }
        public string Schedule { get; set; } = string.Empty; // cron expression (seconds supported by Cronos)
        public int TimeoutSeconds { get; set; } = 0; // 0 = no timeout
        public int Retry { get; set; } = 0;
        public bool OnFailureNotify { get; set; } = false;
        public bool OnSuccessNotify { get; set; } = false;

        // FileCleanup job specific properties
        public string? TargetFolder { get; set; }
        public int FileAgeInDays { get; set; } = 0;
        public bool Recurse { get; set; } = false;
        public string? FileFilter { get; set; }
    }
}
