using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomationEngine.Data.Entities
{
    [Table("JobRuns")]
    public class JobRunEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public JobEntity Job { get; set; } = null!;

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public bool Success { get; set; }

        public int ExitCode { get; set; }

        [MaxLength(2000)]
        public string? StdOut { get; set; }

        [MaxLength(2000)]
        public string? StdErr { get; set; }

        [MaxLength(500)]
        public string? Error { get; set; }

        public int? DurationMs { get; set; }

        // Soft delete support
        public DateTime? DeletedAt { get; set; }
    }
}
