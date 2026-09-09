using AutomationEngine.Models;
using AutomationEngine.Services;

namespace AutomationEngine.Application.Abstractions
{
    /// <summary>
    /// Interface for job execution service
    /// Handles execution of jobs with support for multiple job types
    /// </summary>
    public interface IJobRunner
    {
        /// <summary>
        /// Run a job asynchronously
        /// </summary>
        Task<JobResult> RunAsync(JobConfig job, CancellationToken cancellationToken);
    }
}
