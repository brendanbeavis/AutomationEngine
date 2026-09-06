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

        enum NtfyResult
        {
            Success,
            Failure,
            NotConfigured
        }

        /// <summary>
        /// Send a failure notification via ntfy service
        /// </summary>
        public async Task SendFailureNotificationAsync(string jobDisplayName, string errorMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (string.IsNullOrWhiteSpace(settings.NtfyEndpoint))
                {
                    _logger.LogInformation("ntfy endpoint not configured, skipping notification");
                    return;
                }

                var safeError = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No error details available"
                    : errorMessage.Trim();

                var markdownBody =
                    $"## Job Failed\n\n" +
                    $"**Job:** `{jobDisplayName}`\n\n" +
                    $"**Error:**\n```text\n{safeError}\n```";

                await SendNotificationAsync(jobDisplayName, markdownBody, NtfyResult.Failure, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send ntfy notification | JobName: {JobName}",
                    jobDisplayName);
            }
        }

        /// <summary>
        /// Send a success notification via ntfy service
        /// </summary>
        public async Task SendSuccessNotificationAsync(string jobDisplayName, string errorMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (string.IsNullOrWhiteSpace(settings.NtfyEndpoint))
                {
                    _logger.LogInformation("ntfy endpoint not configured, skipping notification");
                    return;
                }

                var safeError = string.IsNullOrWhiteSpace(errorMessage)
                    ? "No error details available"
                    : errorMessage.Trim();

                var markdownBody =
                    $"## Job Failed\n\n" +
                    $"**Job:** `{jobDisplayName}`\n\n" +
                    $"**Error:**\n```text\n{safeError}\n```";

                await SendNotificationAsync(jobDisplayName, markdownBody, NtfyResult.Success, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send ntfy notification | JobName: {JobName}",
                    jobDisplayName);
            }
        }


        /// <summary>
        /// Send a notification via ntfy service
        /// </summary>
        private async Task SendNotificationAsync(string jobDisplayName, string markdownBody, NtfyResult result, CancellationToken cancellationToken = default)
        {

            var settings = await _settingsService.GetSettingsAsync();

            if (string.IsNullOrWhiteSpace(settings.NtfyEndpoint))
            {
                _logger.LogInformation("ntfy endpoint not configured, skipping notification");
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, settings.NtfyEndpoint)
            {
                Content = new StringContent(markdownBody, Encoding.UTF8, "text/plain")
            };

            request.Headers.TryAddWithoutValidation("Title", $"[AE] Job {result.ToString()}: {jobDisplayName}");
            request.Headers.TryAddWithoutValidation("Priority", "high");
            request.Headers.TryAddWithoutValidation("Tags", result == NtfyResult.Failure ? "warning" : result == NtfyResult.Success ? "white_check_mark" : "");
            request.Headers.TryAddWithoutValidation("Markdown", "yes");

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

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
    }
}
