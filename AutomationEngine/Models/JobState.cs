namespace AutomationEngine.Models
{
    /// <summary>
    /// Represents the current state of a job in the system.
    /// </summary>
    public enum JobState
    {
        /// <summary>
        /// Job is idle and not running
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Job is currently executing
        /// </summary>
        Running = 1,

        /// <summary>
        /// Job is scheduled to run but waiting for scheduler
        /// </summary>
        Scheduled = 2,

        /// <summary>
        /// Job execution failed
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Job execution completed successfully
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Job was forcefully stopped by user
        /// </summary>
        Stopped = 5,

        /// <summary>
        /// Job is disabled
        /// </summary>
        Disabled = 6
    }
}
