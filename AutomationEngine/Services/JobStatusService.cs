using Cronos;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Application.Abstractions;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<JobStatusService> _logger;

        public JobStatusService(ILogger<JobStatusService> logger)
        {
            _logger = logger;
        }

        public string GetNextRun(JobDto job)
        {
            var nextRun = GetNextExecutionTime(job);
            return nextRun.HasValue ? nextRun.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm tt") : "N/A";
        }


        /// <summary>
        /// Generates a human-readable status string describing the job's current state and next execution.
        /// </summary>
        /// <param name="job">The job DTO to get status for.</param>
        /// <returns>A formatted string containing job status information.</returns>
        public string GetJobStatus(JobDto job)
        {
            if (job == null)
            {
                _logger.LogDebug("GetJobStatus called with null job");
                return "Invalid job";
            }

            try
            {
                _logger.LogDebug("Generating status for job | JobId: {JobId} | State: {State}", job.JobId, job.CurrentState);

                // Build status string from components
                var statusParts = new List<string>();

                if (job.IsRunning)
                {
                    statusParts.Add("Job is currently running.");
                }

                if (!job.Enabled)
                {
                    statusParts.Add("Job schedule is disabled!");
                }

                var status = string.Join(" ", statusParts) + (statusParts.Count > 0 ? " " : "");
                _logger.LogDebug("Job status generated | JobId: {JobId} | Status: {Status}", job.JobId, status);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating job status for {JobId}", job.JobId);
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
            {
                _logger.LogDebug("GetNextExecutionTime called with null job or empty schedule");
                return null;
            }

            try
            {
                _logger.LogDebug("Calculating next execution | JobId: {JobId} | Schedule: {Schedule}", job.JobId, job.Schedule);
                CronExpression expression = CronExpression.Parse(job.Schedule);
                DateTime? nextUtc = expression.GetNextOccurrence(DateTime.UtcNow);

                if (nextUtc.HasValue)
                {
                    _logger.LogDebug("Next execution calculated | JobId: {JobId} | NextRun: {NextRun}", 
                        job.JobId, nextUtc.Value.ToLocalTime());
                }

                return nextUtc;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid CRON expression for job | JobId: {JobId} | Schedule: {Schedule}", 
                    job.JobId, job.Schedule);
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
            {
                _logger.LogDebug("GetStatusIndicator called with null job");
                return "Unknown";
            }

            // Prioritize current state over legacy IsRunning flag
            var indicator = job.CurrentState switch
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

            _logger.LogDebug("Status indicator generated | JobId: {JobId} | Indicator: {Indicator}", 
                job.JobId, indicator);
            return indicator;
        }
    }
}

