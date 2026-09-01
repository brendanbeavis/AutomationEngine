using Microsoft.AspNetCore.SignalR;
using System.Diagnostics.CodeAnalysis;

namespace AutomationEngine.SignalR
{
    public class JobStatusHub : Hub
    {
        private readonly ILogger<JobStatusHub> _logger;

        public JobStatusHub(ILogger<JobStatusHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected to JobStatusHub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            _logger.LogInformation("Client {ConnectionId} disconnected from JobStatusHub", Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }

        // Clients can call this method to subscribe to a specific job's updates
        public async Task SubscribeToJob(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"job-{jobId}");
            _logger.LogInformation("Client {ConnectionId} subscribed to job {JobId}", Context.ConnectionId, jobId);
        }

        public async Task UnsubscribeFromJob(string jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job-{jobId}");
            _logger.LogInformation("Client {ConnectionId} unsubscribed from job {JobId}", Context.ConnectionId, jobId);
        }
    }

    /// <summary>
    /// Extension methods for sending job updates to connected clients
    /// </summary>
    public static class JobStatusHubExtensions
    {
        public static async Task BroadcastJobStartedAsync(this IHubContext<JobStatusHub> hub, string jobId, string displayName)
        {
            await hub.Clients.Group($"job-{jobId}").SendAsync("JobStarted", new
            {
                jobId,
                displayName,
                startedAt = DateTime.UtcNow
            });
        }

        public static async Task BroadcastJobCompletedAsync(this IHubContext<JobStatusHub> hub, string jobId, 
            bool success, int exitCode, string? error = null, int? durationMs = null)
        {
            await hub.Clients.Group($"job-{jobId}").SendAsync("JobCompleted", new
            {
                jobId,
                success,
                exitCode,
                error,
                completedAt = DateTime.UtcNow,
                durationMs
            });
        }

        public static async Task BroadcastJobStatusUpdateAsync(this IHubContext<JobStatusHub> hub, string jobId, 
            string status, string? message = null)
        {
            await hub.Clients.Group($"job-{jobId}").SendAsync("JobStatusUpdate", new
            {
                jobId,
                status,
                message,
                timestamp = DateTime.UtcNow
            });
        }

        public static async Task BroadcastJobsListUpdatedAsync(this IHubContext<JobStatusHub> hub)
        {
            await hub.Clients.All.SendAsync("JobsListUpdated", new
            {
                timestamp = DateTime.UtcNow
            });
        }
    }
}
