using AutomationEngine.Api;
using AutomationEngine.Dto;

namespace AutomationEngine.Services
{
    public class JobApiClient
    {
        private readonly HttpClient _http;

        public JobApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<JobDto>?> GetJobsAsync()
        {
            return await _http.GetFromJsonAsync<List<JobDto>>("api/jobs");
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync(JobDto newJob)
        {
            return await _http.PostAsJsonAsync("api/jobs", newJob);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string jobId)
        {
            return await _http.DeleteAsync($"api/jobs/{jobId}");
        }

        public async Task<HttpResponseMessage> TriggerJobAsync(string jobId)
        {
            return await _http.PostAsync($"api/jobs/{jobId}/trigger", null);
        }

        public async Task<List<JobRunDto>?> GetJobHistoryAsync(string jobId)
        {
            return await _http.GetFromJsonAsync<List<JobRunDto>>($"api/jobs/{jobId}/history?limit=50");
        }

        public async Task<HttpResponseMessage> EnableJobAsync(string jobId, bool enabled)
        {
            return await _http.PutAsJsonAsync($"api/jobs/{jobId}/enabled", new SetEnabledDto { Enabled = enabled });
        }

        public async Task<bool> JobIdExistsAsync(string jobId)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<bool>($"api/jobs/{jobId}/exists");
                return result;
            }
            catch
            {
                return false;
            }
        }

        public async Task<HttpResponseMessage> StopJobAsync(string jobId)
        {
            return await _http.PostAsync($"api/jobs/{jobId}/stop", null);
        }

    }
}
