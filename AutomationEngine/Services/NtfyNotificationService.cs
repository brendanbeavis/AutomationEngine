using System.Net.Http;
using System.Text.Json;
using System.Text;
using AutomationEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services
{
    /// <summary>
    /// Service for sending ntfy push notifications
    /// </summary>
    public class NtfyNotificationService : INtfyNotificationService
    {
        private readonly ISettingsService _settingsService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<NtfyNotificationService> _logger;

        public NtfyNotificationService(
            ISettingsService settingsService,
            HttpClient httpClient,
            ILogger<NtfyNotificationService> logger)
        {
            _settingsService = settingsService;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Send a failure notification via ntfy service
        /// </summary>
        public async Task SendFailureNotificationAsync(string jobDisplayName, string errorMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get settings from database
                var settings = await _settingsService.GetSettingsAsync();

                // Exit early if ntfy endpoint is not configured
                if (string.IsNullOrEmpty(settings.NtfyEndpoint))
                {
                    _logger.LogDebug("ntfy endpoint not configured, skipping notification");
                    return;
                }

                // Prepare notification payload
                var payload = new
                {
                    title = $"Job failed: {jobDisplayName}",
                    message = errorMessage ?? "No error details available"
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send notification
                var response = await _httpClient.PostAsync(settings.NtfyEndpoint, content, cancellationToken)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "ntfy notification failed with status {StatusCode} | JobName: {JobName}",
                        response.StatusCode, jobDisplayName);
                }
                else
                {
                    _logger.LogInformation(
                        "ntfy notification sent successfully | JobName: {JobName}",
                        jobDisplayName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send ntfy notification | JobName: {JobName}",
                    jobDisplayName);
            }
        }
    }
}
