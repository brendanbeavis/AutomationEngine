using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AutomationEngine.Models;

namespace AutomationEngine.Data.Entities
{
    [Table("Jobs")]
    public class JobEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string JobId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Command { get; set; } = string.Empty;

        [MaxLength(500)]
        // Optional script content when Type == Process
        public string? Arguments { get; set; }

        [MaxLength(200)]
        public string? WorkingDirectory { get; set; }

        // Persisted job type: Process (0) or PowerShell (1)
        public JobType Type { get; set; } = JobType.Process;

        // Optional script content when Type == PowerShell
        public string? Script { get; set; }

        [Required]
        [MaxLength(50)]
        public string Schedule { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 0;

        public int Retry { get; set; } = 0;
        public bool OnFailureNotify { get; set; } = false;

        public bool Enabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // FileCleanup job specific columns
        [MaxLength(500)]
        public string? TargetFolder { get; set; }
        public int FileAgeInDays { get; set; } = 0;
        public bool Recurse { get; set; } = false;
        [MaxLength(100)]
        public string? FileFilter { get; set; }

        // Job state tracking
        public JobState CurrentState { get; set; } = JobState.Idle;
        public DateTime? RunningStartTime { get; set; }
        public int? RunningProcessId { get; set; }

        // Soft delete support
        public DateTime? DeletedAt { get; set; }

        public ICollection<JobRunEntity> Runs { get; set; } = new List<JobRunEntity>();
    }
}
