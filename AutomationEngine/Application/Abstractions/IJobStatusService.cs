using AutomationEngine.Dto;

namespace AutomationEngine.Application.Abstractions
{
    /// <summary>
    /// Service for generating human-readable job status information.
    /// Single Responsibility: Generate status strings describing job state and schedule
    /// </summary>
    /// <remarks>
    /// Provides presentation-layer logic for displaying job status, including:
    /// - Current running state
    /// - Next scheduled execution time
    /// - Enabled/disabled status
    /// 
    /// This service extracts presentation logic from the DTO to maintain proper separation of concerns.
    /// </remarks>
    public interface IJobStatusService
    {
        /// <summary>
        /// Generates a human-readable status string describing the job's current state and next execution.
        /// </summary>
        /// <param name="job">The job DTO to get status for.</param>
        /// <returns>A formatted string containing job status information including running state, next scheduled run, and enabled status.</returns>
        /// <remarks>
        /// Returns a concatenated string with one or more of the following components:
        /// - "Job is currently running; " (if IsRunning is true)
        /// - "Next run: [DateTime]; " (if Schedule is valid and next occurrence exists)
        /// - "However job schedule is disabled!; " (if Enabled is false)
        /// 
        /// Example: "Next run: 2024-10-15 14:30:00; However job schedule is disabled!; "
        /// </remarks>
        string GetJobStatus(JobDto job);

        /// <summary>
        /// Calculates the next execution time for a job based on its CRON schedule.
        /// </summary>
        /// <param name="job">The job DTO to calculate the next run time for.</param>
        /// <returns>A DateTime in local time representing the next execution, or null if the schedule is invalid.</returns>
        /// <remarks>
        /// Returns null if:
        /// - The job's Schedule property is null or empty
        /// - The schedule is not a valid CRON expression
        /// </remarks>
        DateTime? GetNextExecutionTime(JobDto job);

        /// <summary>
        /// Gets a brief status indicator showing the current job state.
        /// </summary>
        /// <param name="job">The job DTO to get the status indicator for.</param>
        /// <returns>A status indicator string such as "Running", "Idle", "Queued", "Completed".</returns>
        string GetStatusIndicator(JobDto job);
    }
}
