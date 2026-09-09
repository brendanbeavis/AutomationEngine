using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Services
{
    /// <summary>
    /// A safe, observable background task queue for fire-and-forget operations.
    /// Ensures all exceptions are properly logged and tracked.
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// Queue a background task for execution
        /// </summary>
        ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);

        /// <summary>
        /// Queue a background task with a specific job ID for tracking/logging
        /// </summary>
        ValueTask QueueAsync(string jobId, Func<CancellationToken, ValueTask> workItem);
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue, IHostedService
    {
        private readonly Channel<WorkItem> _queue;
        private readonly ILogger<BackgroundTaskQueue> _logger;
        private CancellationTokenSource? _cts;
        private Task? _processingTask;

        private class WorkItem
        {
            public required string? JobId { get; set; }
            public required Func<CancellationToken, ValueTask> Work { get; set; }
        }

        public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger)
        {
            _logger = logger;
            _queue = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });
        }

        public async ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem)
        {
            await QueueAsync(null, workItem).ConfigureAwait(false);
        }

        public async ValueTask QueueAsync(string jobId, Func<CancellationToken, ValueTask> workItem)
        {
            ArgumentNullException.ThrowIfNull(workItem);

            await _queue.Writer.WriteAsync(new WorkItem
            {
                JobId = jobId,
                Work = workItem
            }).ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _processingTask = ProcessQueueAsync(_cts.Token);
            _logger.LogInformation("BackgroundTaskQueue started");
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackgroundTaskQueue stopping");
            _queue.Writer.Complete();

            if (_processingTask is not null)
            {
                await _processingTask.ConfigureAwait(false);
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _logger.LogInformation("BackgroundTaskQueue stopped");
        }

        private async Task ProcessQueueAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var workItem in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        var jobIdContext = workItem.JobId != null ? $"JobId: {workItem.JobId}" : "No JobId";
                        _logger.LogDebug("Executing background task: {JobIdContext}", jobIdContext);

                        await workItem.Work(stoppingToken).ConfigureAwait(false);

                        _logger.LogDebug("Background task completed: {JobIdContext}", jobIdContext);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Background task cancelled for JobId: {JobId}", workItem.JobId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception in background task for JobId: {JobId}", workItem.JobId);
                        // Don't rethrow - we want to continue processing other tasks
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BackgroundTaskQueue processing cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error in BackgroundTaskQueue processing loop");
                throw;
            }
        }
    }
}
