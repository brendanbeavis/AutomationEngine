using System;
using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Dto
{
    /// <summary>
    /// Data Transfer Object representing a single job execution run.
    /// </summary>
    /// <remarks>
    /// Contains metadata and execution results for a specific job run, including start/completion times,
    /// exit codes, and captured output. Used for tracking job execution history and displaying run details.
    /// </remarks>
    public class JobRunDto
    {
        /// <summary>
        /// Unique identifier for the job run.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive number")]
        public int Id { get; set; }

        /// <summary>
        /// Timestamp when the job run started.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Timestamp when the job run completed. Null if the job is still running.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Indicates whether the job run completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Exit code returned by the job process.
        /// </summary>
        [Range(-1, 65535, ErrorMessage = "ExitCode must be between -1 and 65535")]
        public int ExitCode { get; set; }

        /// <summary>
        /// Standard output captured from the job execution.
        /// </summary>
        [StringLength(1000000, ErrorMessage = "StdOut is limited to 1,000,000 characters")]
        public string StdOut { get; set; } = "";

        /// <summary>
        /// Standard error output captured from the job execution.
        /// </summary>
        [StringLength(1000000, ErrorMessage = "StdErr is limited to 1,000,000 characters")]
        public string StdErr { get; set; } = "";

        /// <summary>
        /// Duration of job execution in milliseconds. Null if the job is still running.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "DurationMs must be non-negative")]
        public int? DurationMs { get; set; }
    }
}
