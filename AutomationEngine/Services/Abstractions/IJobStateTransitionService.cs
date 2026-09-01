using AutomationEngine.Models;

namespace AutomationEngine.Services.Abstractions
{
    /// <summary>
    /// Service for managing job state transitions
    /// Single Responsibility: State transitions and validation only
    /// Handles state machine logic for job lifecycle
    /// </summary>
    public interface IJobStateTransitionService
    {
        /// <summary>
        /// Transition a job to a new state
        /// </summary>
        Task TransitionToStateAsync(string jobId, JobState newState, int? processId = null);

        /// <summary>
        /// Transition a job to Running state with process ID
        /// </summary>
        Task TransitionToRunningAsync(string jobId, int processId);

        /// <summary>
        /// Transition a job to Completed state
        /// </summary>
        Task TransitionToCompletedAsync(string jobId);

        /// <summary>
        /// Transition a job to Failed state
        /// </summary>
        Task TransitionToFailedAsync(string jobId);

        /// <summary>
        /// Transition a job to Stopped state
        /// </summary>
        Task TransitionToStoppedAsync(string jobId);

        /// <summary>
        /// Get the current state of a job
        /// </summary>
        Task<JobState?> GetCurrentStateAsync(string jobId);

        /// <summary>
        /// Validate if a state transition is allowed
        /// </summary>
        bool IsValidTransition(JobState currentState, JobState newState);

        /// <summary>
        /// Get allowed next states for a given current state
        /// </summary>
        IEnumerable<JobState> GetAllowedNextStates(JobState currentState);

        /// <summary>
        /// Check if a process with the given ID still exists
        /// </summary>
        bool IsProcessStillRunning(int processId);

        /// <summary>
        /// Validate and cleanup stale Running states where the process no longer exists
        /// </summary>
        Task ValidateAndCleanupStaleRunningStatesAsync();
    }
}
