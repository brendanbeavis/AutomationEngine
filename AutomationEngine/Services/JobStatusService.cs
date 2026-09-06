using Cronos;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Services.Abstractions;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Service implementation for generating human-readable job status information.
    /// </summary>
    /// <remarks>
    /// Extracted from JobDto to maintain separation of concerns and keep DTOs lightweight.
    /// Provides presentation-layer logic for displaying job state and schedule information.
    /// </remarks>
    public class JobStatusService : IJobStatusService
    {
        /// <summary>
        /// Generates a human-readable status string describing the job's current state and next execution.
        /// </summary>
        /// <param name="job">The job DTO to get status for.</param>
        /// <returns>A formatted string containing job status information.</returns>
        public string GetJobStatus(JobDto job)
        {
            if (job == null)
                return "Invalid job";

            try
            {
                // Build status string from components
                var statusParts = new List<string>();

                if (job.IsRunning)
                {
                    statusParts.Add("Job is currently running.");
                }

                var nextRun = GetNextExecutionTime(job);
                if (nextRun.HasValue)
                {
                    statusParts.Add($"Next run: {nextRun.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}.");
                }

                if (!job.Enabled)
                {
                    statusParts.Add("However job schedule is disabled!");
                }

                return string.Join(" ", statusParts) + (statusParts.Count > 0 ? " " : "");
            }
            catch
            {
                return "Unable to determine job status.";
            }
        }

        /// <summary>
        /// Calculates the next execution time for a job based on its CRON schedule.
        /// </summary>
        /// <param name="job">The job DTO to calculate the next run time for.</param>
        /// <returns>A DateTime in local time representing the next execution, or null if the schedule is invalid.</returns>
        public DateTime? GetNextExecutionTime(JobDto job)
        {
            if (job == null || string.IsNullOrWhiteSpace(job.Schedule))
                return null;

            try
            {
                CronExpression expression = CronExpression.Parse(job.Schedule);
                DateTime? nextUtc = expression.GetNextOccurrence(DateTime.UtcNow);
                return nextUtc;
            }
            catch
            {
                // Invalid CRON expression - return null
                return null;
            }
        }

        /// <summary>
        /// Gets a brief status indicator showing the current job state.
        /// </summary>
        /// <param name="job">The job DTO to get the status indicator for.</param>
        /// <returns>A status indicator string.</returns>
        public string GetStatusIndicator(JobDto job)
        {
            if (job == null)
                return "Unknown";

            // Prioritize current state over legacy IsRunning flag
            return job.CurrentState switch
            {
                JobState.Idle => "Idle",
                JobState.Running => "Running",
                JobState.Scheduled => "Scheduled",
                JobState.Failed => "Failed",
                JobState.Completed => "Completed",
                JobState.Stopped => "Stopped",
                JobState.Disabled => "Disabled",
                _ => job.IsRunning ? "Running" : "Idle"
            };
        }
    }
}
