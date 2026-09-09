namespace AutomationEngine.Application.Abstractions
{
    /// <summary>
    /// Service for sending ntfy push notifications
    /// </summary>
    public interface INtfyNotificationService
    {
        /// <summary>
        /// Send a failure notification via ntfy service
        /// </summary>
        /// <param name="jobDisplayName">Display name of the job that failed</param>
        /// <param name="errorMessage">Error message from job failure</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task SendFailureNotificationAsync(string jobDisplayName, string errorMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a success notification via ntfy service
        /// </summary>
        /// <param name="jobDisplayName">Display name of the job that succeeded</param>
        /// <param name="details">Optional success details from job output</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task SendSuccessNotificationAsync(string jobDisplayName, string details, CancellationToken cancellationToken = default);
    }
}
