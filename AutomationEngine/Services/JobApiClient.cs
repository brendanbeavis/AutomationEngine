using AutomationEngine.Api;
using AutomationEngine.Dto;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services
{
    public class JobApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<JobApiClient> _logger;

        public JobApiClient(HttpClient http, ILogger<JobApiClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<JobDto>?> GetJobsAsync()
        {
            try
            {
                _logger.LogDebug("Fetching all jobs from API");
                var jobs = await _http.GetFromJsonAsync<List<JobDto>>("api/jobs");
                _logger.LogDebug("Retrieved {JobCount} jobs from API", jobs?.Count ?? 0);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching jobs from API");
                throw;
            }
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync(JobDto newJob)
        {
            try
            {
                _logger.LogInformation("Creating new job via API | JobId: {JobId} | DisplayName: {DisplayName}", 
                    newJob.JobId, newJob.DisplayName);
                var response = await _http.PostAsJsonAsync("api/jobs", newJob);
                _logger.LogDebug("Job creation response | StatusCode: {StatusCode}", response.StatusCode);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job via API | JobId: {JobId}", newJob.JobId);
                throw;
            }
        }

        public async Task<HttpResponseMessage> DeleteAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("Deleting job via API | JobId: {JobId}", jobId);
                var response = await _http.DeleteAsync($"api/jobs/{jobId}");
                _logger.LogDebug("Job deletion response | StatusCode: {StatusCode}", response.StatusCode);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job via API | JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task<HttpResponseMessage> TriggerJobAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("Triggering job via API | JobId: {JobId}", jobId);
                var response = await _http.PostAsync($"api/jobs/{jobId}/trigger", null);
                _logger.LogDebug("Job trigger response | StatusCode: {StatusCode}", response.StatusCode);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering job via API | JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task<List<JobRunDto>?> GetJobHistoryAsync(string jobId)
        {
            try
            {
                _logger.LogDebug("Fetching job history via API | JobId: {JobId}", jobId);
                var history = await _http.GetFromJsonAsync<List<JobRunDto>>($"api/jobs/{jobId}/history?limit=50");
                _logger.LogDebug("Retrieved {RunCount} job runs | JobId: {JobId}", history?.Count ?? 0, jobId);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job history via API | JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task<HttpResponseMessage> EnableJobAsync(string jobId, bool enabled)
        {
            try
            {
                _logger.LogInformation("Setting job enabled state via API | JobId: {JobId} | Enabled: {Enabled}", 
                    jobId, enabled);
                var response = await _http.PutAsJsonAsync($"api/jobs/{jobId}/enabled", new SetEnabledDto { Enabled = enabled });
                _logger.LogDebug("Enable job response | StatusCode: {StatusCode}", response.StatusCode);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting job enabled state via API | JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task<bool> JobIdExistsAsync(string jobId)
        {
            try
            {
                _logger.LogDebug("Checking if job exists via API | JobId: {JobId}", jobId);
                var result = await _http.GetFromJsonAsync<bool>($"api/jobs/{jobId}/exists");
                _logger.LogDebug("Job existence check result | JobId: {JobId} | Exists: {Exists}", jobId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking job existence via API | JobId: {JobId}, assuming not exists", jobId);
                return false;
            }
        }

        public async Task<HttpResponseMessage> StopJobAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("Stopping job via API | JobId: {JobId}", jobId);
                var response = await _http.PostAsync($"api/jobs/{jobId}/stop", null);
                _logger.LogDebug("Job stop response | StatusCode: {StatusCode}", response.StatusCode);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping job via API | JobId: {JobId}", jobId);
                throw;
            }
        }

    }
}
