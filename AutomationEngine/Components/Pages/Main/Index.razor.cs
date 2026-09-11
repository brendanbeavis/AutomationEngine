using AutomationEngine.Configuration;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Models.Cron;
using AutomationEngine.Services;
using AutomationEngine.Application.Abstractions;
using AutomationEngine.Utilities.Cron;
using Cronos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AutomationEngine.Components.Pages.Main
{
    /// <summary>
    /// Represents job completion data received from SignalR
    /// </summary>
    public class JobCompletionData
    {
        public string jobId { get; set; } = string.Empty;
        public bool success { get; set; }
    }

    public partial class Index : ComponentBase, IAsyncDisposable
    {
        [Inject]
        private HttpClient Http { get; set; } = default!;

        [Inject]
        private NavigationManager NavManager { get; set; } = default!;

        [Inject]
        private JobApiClient JobApi { get; set; } = default!;

        [Inject]
        private ILogger<Index> _logger { get; set; } = default!;

        [Inject]
        private IJobStatusService JobStatusService { get; set; } = default!;

        // SignalR Hub Connection
        private HubConnection? _hubConnection;
        private Dictionary<string, TaskCompletionSource<bool>> _jobCompletionSources = new();

        [Parameter]
        public CronSchedule schedule { get; set; } = new();

        private List<JobDto>? jobs;
        private bool showAddJobModal = false;
        private bool showHistoryModal = false;
        private bool isEdit = false;
        private bool isDupe = false;
        private JobDto newJob = new JobDto();
        private string oldCommand = string.Empty;
        private EditContext editContext = new EditContext(new JobDto());
        private string modalErrorMessage = string.Empty;
        private string historyJobId = string.Empty;
        private List<JobRunDto>? runs;

        private bool IsBtnTriggerJobDisabled { get; set; } = false;
        private bool IsBtnRefreshDisabled { get; set; } = true;

        private bool showDeleteConfirm = false;
        private string deleteJobId = string.Empty;
        private string deleteJobDisplayName = string.Empty;

        private bool showStopConfirm = false;
        private string stopJobId = string.Empty;
        private string stopJobDisplayName = string.Empty;

        private bool showSettingsModal = false;

        // Notification System
        private string notificationMessage = string.Empty;
        private string notificationType = "success"; // success, danger, warning
        private bool isNotificationVisible = false;
        private CancellationTokenSource? notificationCts;
        private Task? _autoHideTask = null;

        // Job Summary Statistics
        private int TotalJobs { get; set; } = 0;
        private int TotalEnabled { get; set; } = 0;
        private int TotalDisabled { get; set; } = 0;
        private int TotalFailed { get; set; } = 0;
        private int TotalSucceeded { get; set; } = 0;
        private int TotalRunning { get; set; } = 0;
        private int TotalQueued { get; set; } = 0;

        protected override async Task OnInitializedAsync()
        {
            _logger.LogInformation("Index component initializing");
            // Initialize SignalR connection
            await InitializeSignalRAsync();
            _logger.LogInformation("Index component initialized successfully");

            await RefreshJobs();
        }

        private async Task InitializeSignalRAsync()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(NavManager.ToAbsoluteUri(Constants.SignalR.JobStatusHubPath))
                    .WithAutomaticReconnect()
                    .Build();

                // Register event handlers for job status updates
                _hubConnection.On<JobCompletionData>("JobCompleted", async (jobData) =>
                {
                    await HandleJobCompletedAsync(jobData);
                });

                _hubConnection.On<dynamic>("JobsListUpdated", async (data) =>
                {
                    await RefreshJobs();
                });

                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SignalR connection");
            }
        }

        private async Task RefreshJobs()
        {
            IsBtnRefreshDisabled = true;
            try
            {
                jobs = await JobApi.GetJobsAsync();
                jobs ??= new List<JobDto>();
                CalculateJobSummary();
                ShowNotification("Jobs reloaded successfully!", "success", 2000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh jobs from API");
                ShowNotification($"Error loading jobs: {ex.Message}", "danger", 0);
            }
            await Task.Delay(500);
            IsBtnRefreshDisabled = false;
        }

        private async Task TriggerJob(JobDto job)
        {
            job.IsRunning = true;
            try
            {
                // Subscribe to job updates via SignalR before triggering
                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("SubscribeToJob", job.JobId);

                    // Create a completion source to wait for job completion
                    var completionSource = new TaskCompletionSource<bool>();
                    _jobCompletionSources[job.JobId] = completionSource;

                    // Set a timeout of 5 minutes for job completion
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

                    try
                    {
                        // Trigger the job
                        var response = await JobApi.TriggerJobAsync(job.JobId);
                        if (response.IsSuccessStatusCode)
                        {
                            ShowNotification($"Job '{job.DisplayName}' triggered successfully", "success", 3000);

                            // Wait for job completion or timeout
                            await completionSource.Task.ConfigureAwait(false);
                        }
                        else
                        {
                            ShowNotification($"Error triggering job: {response.StatusCode}", "danger", 0);
                            job.IsRunning = false;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        ShowNotification($"Job timeout or connection issue", "warning", 0);
                        job.IsRunning = false;
                    }
                    finally
                    {
                        // Clean up and unsubscribe
                        _jobCompletionSources.Remove(job.JobId);
                        if (_hubConnection?.State == HubConnectionState.Connected)
                        {
                            await _hubConnection.InvokeAsync("UnsubscribeFromJob", job.JobId);
                        }
                    }
                }
                else
                {
                    // Fallback if SignalR is not connected
                    var response = await JobApi.TriggerJobAsync(job.JobId);
                    if (response.IsSuccessStatusCode)
                    {
                        ShowNotification($"Job '{job.DisplayName}' triggered successfully", "success", 3000);
                        await Task.Delay(2000);
                    }
                    else
                    {
                        ShowNotification($"Error triggering job: {response.StatusCode}", "danger", 0);
                    }
                    job.IsRunning = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger job | JobId: {JobId}", job.JobId);
                ShowNotification($"Error triggering job: {ex.Message}", "danger", 0);
                job.IsRunning = false;
            }
        }

        private async Task DeleteJob(string jobId)
        {
            try
            {
                var response = await JobApi.DeleteAsync(jobId);
                if (response.IsSuccessStatusCode)
                {
                    ShowNotification("Job deleted successfully", "success", 3000);
                    await RefreshJobs();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    ShowNotification("Cannot delete a running job. Please stop it first.", "warning", 0);
                }
                else
                {
                    ShowNotification($"Error deleting job: {response.StatusCode} - {response.ReasonPhrase}", "danger", 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete job | JobId: {JobId}", jobId);
                ShowNotification($"Error deleting job: {ex.Message}", "danger", 0);
            }
        }

        private async Task StopJob(string jobId)
        {
            try
            {
                var response = await JobApi.StopJobAsync(jobId);
                if (response.IsSuccessStatusCode)
                {
                    ShowNotification("Job stopped successfully", "success", 3000);
                    await RefreshJobs();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    ShowNotification("Job is not currently running", "warning", 0);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ShowNotification("Job not found", "danger", 0);
                }
                else
                {
                    ShowNotification($"Error stopping job: {response.StatusCode} - {response.ReasonPhrase}", "danger", 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop job | JobId: {JobId}", jobId);
                ShowNotification($"Error stopping job: {ex.Message}", "danger", 0);
            }
        }

        private async Task EnableDisableJob(string jobId, bool jobEnabled)
        {
            try
            {
                jobEnabled = !jobEnabled;
                var response = await JobApi.EnableJobAsync(jobId, jobEnabled);
                if (response.IsSuccessStatusCode)
                {
                    string status = jobEnabled ? "enabled" : "disabled";
                    ShowNotification($"Job {status} successfully", "success", 3000);
                    await RefreshJobs();
                }
                else
                {
                    ShowNotification($"Error updating job: {response.StatusCode}", "danger", 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update job enabled status | JobId: {JobId}", jobId);
                ShowNotification($"Error updating job: {ex.Message}", "danger", 0);
            }
        }

        private void ShowAddJobModal()
        {
            isEdit = false;
            newJob = new JobDto();
            newJob.Schedule = CronExpressionGenerator.Generate(schedule);
            editContext = new EditContext(newJob);
            oldCommand = newJob.Command;
            modalErrorMessage = string.Empty;
            showAddJobModal = true;
        }

        private void ShowEditJobModal(JobDto job)
        {
            isEdit = true;
            isDupe = false;
            newJob = new JobDto
            {
                JobId = job.JobId,
                DisplayName = job.DisplayName,
                Type = job.Type,
                Command = job.Command,
                Arguments = job.Arguments,
                Script = job.Script,
                WorkingDirectory = job.WorkingDirectory,
                Schedule = job.Schedule,
                TimeoutSeconds = job.TimeoutSeconds,
                Retry = job.Retry,
                SuccessExitCodes = job.SuccessExitCodes,
                OnFailureNotify = job.OnFailureNotify,
                OnSuccessNotify = job.OnSuccessNotify,
                Enabled = job.Enabled,
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt,
                TargetFolder = job.TargetFolder,
                FileAgeInDays = job.FileAgeInDays,
                Recurse = job.Recurse,
                FileFilter = job.FileFilter
            };
            schedule = CronScheduleParser.FromExpression(job.Schedule);
            editContext = new EditContext(newJob);
            modalErrorMessage = string.Empty;
            showAddJobModal = true;
        }

        private void ShowDeleteConfirm(JobDto job)
        {
            deleteJobId = job.JobId;
            deleteJobDisplayName = job.DisplayName;
            showDeleteConfirm = true;
        }

        private void HideDeleteConfirm()
        {
            showDeleteConfirm = false;
            deleteJobId = string.Empty;
            deleteJobDisplayName = string.Empty;
        }

        private async Task OnDeleteConfirmed()
        {
            await DeleteJob(deleteJobId);
            HideDeleteConfirm();
        }

        private void ShowStopConfirm(JobDto job)
        {
            stopJobId = job.JobId;
            stopJobDisplayName = job.DisplayName;
            showStopConfirm = true;
        }

        private void HideStopConfirm()
        {
            showStopConfirm = false;
            stopJobId = string.Empty;
            stopJobDisplayName = string.Empty;
        }

        private async Task OnStopConfirmed()
        {
            await StopJob(stopJobId);
            HideStopConfirm();
        }

        private void HideAddJobModal()
        {
            showAddJobModal = false;
            newJob = new JobDto();
            editContext = new EditContext(newJob);
        }

        private async Task ShowHistoryModal(string jobId)
        {
            historyJobId = jobId;
            showHistoryModal = true;
            try
            {
                runs = await JobApi.GetJobHistoryAsync(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load job history | JobId: {JobId}", jobId);
                runs = new List<JobRunDto>();
            }
        }

        private void HideHistoryModal()
        {
            showHistoryModal = false;
            historyJobId = string.Empty;
            runs = null;
        }

        private void ShowDuplicateModal(JobDto job)
        {
            isEdit = false;
            isDupe = true;
            newJob = new JobDto
            {
                DisplayName = $"{job.DisplayName} (Copy)",
                Type = job.Type,
                Command = job.Command,
                Arguments = job.Arguments,
                Script = job.Script,
                WorkingDirectory = job.WorkingDirectory,
                Schedule = job.Schedule,
                TimeoutSeconds = job.TimeoutSeconds,
                Retry = job.Retry,
                SuccessExitCodes = job.SuccessExitCodes,
                OnFailureNotify = job.OnFailureNotify,
                OnSuccessNotify = job.OnSuccessNotify,
                Enabled = job.Enabled,
                TargetFolder = job.TargetFolder,
                FileAgeInDays = job.FileAgeInDays,
                Recurse = job.Recurse,
                FileFilter = job.FileFilter
            };
            schedule = CronScheduleParser.FromExpression(job.Schedule);
            editContext = new EditContext(newJob);
            modalErrorMessage = string.Empty;
            showAddJobModal = true;
        }

        private void ShowSettings()
        {
            showSettingsModal = true;
        }

        private void HideSettingsModal()
        {
            showSettingsModal = false;
        }

        // Modal Callback Methods
        private async Task OnAddJobSaved(JobDto job)
        {
            try
            {
                var response = await JobApi.PostAsJsonAsync(job);
                if (response.IsSuccessStatusCode)
                {
                    string action = isEdit ? "updated" : "created";
                    ShowNotification($"Job {action} successfully", "success", 3000);
                    await RefreshJobs();
                    HideAddJobModal();
                }
                else
                {
                    modalErrorMessage = $"Error saving job: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save job");
                modalErrorMessage = $"Error saving job: {ex.Message}";
            }
        }

        private void OnAddJobClosed()
        {
            HideAddJobModal();
        }

        private void OnHistoryModalClosed()
        {
            HideHistoryModal();
        }

        private void OnSettingsModalClosed()
        {
            HideSettingsModal();
        }

        // Notification Management
        private void ShowNotification(string message, string type = "success", int autoHideDuration = 3000)
        {
            // Cancel any existing auto-hide timer
            if (notificationCts is not null)
            {
                try
                {
                    notificationCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Token was already disposed, ignore
                }
                finally
                {
                    notificationCts.Dispose();
                    notificationCts = null;
                }
            }

            notificationMessage = message;
            notificationType = type;
            isNotificationVisible = true;

            if (autoHideDuration > 0)
            {
                notificationCts = new CancellationTokenSource();
                // Fire and forget, but capture the task to ensure proper cleanup
                _autoHideTask = AutoHideNotificationAsync(autoHideDuration, notificationCts.Token);
            }
        }

        private void HideNotification()
        {
            // Cancel any pending auto-hide
            if (notificationCts is not null)
            {
                try
                {
                    notificationCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Token was already disposed, ignore
                }
                finally
                {
                    notificationCts.Dispose();
                    notificationCts = null;
                }
            }

            isNotificationVisible = false;
            notificationMessage = string.Empty;
        }

        private async Task AutoHideNotificationAsync(int ms, CancellationToken ct)
        {
            try
            {
                await Task.Delay(ms, ct);
                if (!ct.IsCancellationRequested)
                {
                    isNotificationVisible = false;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (OperationCanceledException)
            {
                // Notification was manually hidden or replaced
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource was disposed, notification already replaced
            }
        }

        // SignalR Job Completion Handler
        private async Task HandleJobCompletedAsync(JobCompletionData jobData)
        {
            try
            {
                string jobId = jobData.jobId;
                bool success = jobData.success;

                // Update UI
                await InvokeAsync(async () =>
                {
                    // Find the job and update its running state
                    var job = jobs?.FirstOrDefault(j => j.JobId == jobId);
                    if (job is not null)
                    {
                        job.IsRunning = false;
                        job.LastRunSuccess = success;

                        // Show completion notification
                        string message = success 
                            ? $"Job '{job.DisplayName}' completed successfully" 
                            : $"Job '{job.DisplayName}' failed";
                        string type = success ? "success" : "danger";
                        ShowNotification(message, type, 5000);

                        // Refresh to get updated stats
                        await RefreshJobs();
                    }

                    StateHasChanged();
                });

                // Signal job completion if there's a waiting task
                if (_jobCompletionSources.TryGetValue(jobId, out var completionSource))
                {
                    completionSource.TrySetResult(success);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling job completion notification");
            }
        }

        // SignalR Connection Cleanup
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}

