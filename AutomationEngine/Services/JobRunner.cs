using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutomationEngine.Data.Entities;
using AutomationEngine.Extensions;
using AutomationEngine.Models;
using AutomationEngine.Services.Abstractions;
using AutomationEngine.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace AutomationEngine.Services
{
    public class JobResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string StdOut { get; set; } = string.Empty;
        public string StdErr { get; set; } = string.Empty;
    }

    public class JobRunner : IJobRunner
    {
        private readonly ILogger<JobRunner> _logger;
        private readonly JobStateManager? _stateManager;
        private readonly IHubContext<JobStatusHub>? _hubContext;
        private readonly AuditLogService? _auditService;

        public JobRunner(ILogger<JobRunner> logger, JobStateManager? stateManager = null, IHubContext<JobStatusHub>? hubContext = null, AuditLogService? auditService = null)
        {
            _logger = logger;
            _stateManager = stateManager;
            _hubContext = hubContext;
            _auditService = auditService;
        }

        public async Task<JobResult> RunAsync(JobConfig job, CancellationToken cancellationToken)
        {
            // Validate input parameters
            ArgumentNullException.ThrowIfNull(job);

            if (string.IsNullOrWhiteSpace(job.Id))
            {
                throw new ArgumentException("Job ID cannot be empty or whitespace", nameof(job.Id));
            }

            if (string.IsNullOrWhiteSpace(job.DisplayName))
            {
                throw new ArgumentException("Job DisplayName cannot be empty or whitespace", nameof(job.DisplayName));
            }

            // Validate job type specific requirements
            if (job.Type == Models.JobType.Process)
            {
                if (string.IsNullOrWhiteSpace(job.Command))
                {
                    throw new ArgumentException("Command is required for Process jobs", nameof(job.Command));
                }
                if (job.Command.Length > 500)
                {
                    throw new ArgumentException("Command exceeds maximum length of 500 characters", nameof(job.Command));
                }
            }

            if (job.Type == Models.JobType.PowerShell)
            {
                if (string.IsNullOrWhiteSpace(job.Script))
                {
                    throw new ArgumentException("Script is required for PowerShell jobs", nameof(job.Script));
                }
                if (job.Script.Length > 5000)
                {
                    throw new ArgumentException("Script exceeds maximum length of 5000 characters", nameof(job.Script));
                }
            }

            if (job.Type == Models.JobType.FileCleanup)
            {
                if (string.IsNullOrWhiteSpace(job.TargetFolder))
                {
                    throw new ArgumentException("TargetFolder is required for FileCleanup jobs", nameof(job.TargetFolder));
                }
                if (!IsValidFilePath(job.TargetFolder))
                {
                    throw new ArgumentException("TargetFolder contains invalid path characters", nameof(job.TargetFolder));
                }
            }

            // Validate timeout
            if (job.TimeoutSeconds < 0)
            {
                throw new ArgumentException("TimeoutSeconds cannot be negative", nameof(job.TimeoutSeconds));
            }
            if (job.TimeoutSeconds > 86400) // 24 hours
            {
                throw new ArgumentException("TimeoutSeconds exceeds maximum of 86400 (24 hours)", nameof(job.TimeoutSeconds));
            }

            // Validate retry count
            if (job.Retry < 0 || job.Retry > 100)
            {
                throw new ArgumentException("Retry count must be between 0 and 100", nameof(job.Retry));
            }

            // Validate working directory if provided
            if (!string.IsNullOrWhiteSpace(job.WorkingDirectory) && !IsValidFilePath(job.WorkingDirectory))
            {
                throw new ArgumentException("WorkingDirectory contains invalid path characters", nameof(job.WorkingDirectory));
            }

            var stopwatch = Stopwatch.StartNew();
            var correlationId = LoggingContext.GetOrCreateCorrelationId();

            using (LoggingContext.CreateActivityScope(job.Id, "JobExecution"))
            {
                StructuredLogger.LogJobStarted(_logger, job.Id, job.DisplayName, job.Type.ToString());

                try
                {
                    // Use Polly to retry failed runs (based on job.Retry). Retries are applied when the job result has Success == false.
                    var retryCount = Math.Max(0, job.Retry);

                    var policy = Policy<JobResult>
                        .HandleResult(r => !r.Success)
                        .WaitAndRetryAsync(retryCount, 
                            attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), 
                            onRetry: async (outcome, timespan, retryAttempt, ctx) =>
                            {
                                StructuredLogger.LogRetryAttempt(_logger, job.Id, retryAttempt, retryCount + 1, 
                                    timespan, new Exception(outcome.Result?.StdErr ?? "Unknown error"));

                                // Log audit event for retry attempt
                                if (_auditService != null && job.JobDatabaseId.HasValue)
                                {
                                    var errorMsg = outcome.Result?.StdErr;
                                    await _auditService.LogAuditEventAsync(
                                        job.JobDatabaseId.Value,
                                        AuditLogActions.JobRetrying,
                                        description: $"Retrying job (attempt {retryAttempt} of {retryCount + 1}, waiting {(int)timespan.TotalSeconds}s)",
                                        errorMessage: errorMsg != null ? errorMsg.Truncate(500) : null,
                                        details: new { attemptNumber = retryAttempt, totalRetries = retryCount + 1, waitSeconds = (int)timespan.TotalSeconds }
                                    );
                                }
                            });

                    var result = await policy.ExecuteAsync(ct => ExecuteOnceAsync(job, ct), cancellationToken)
                        .ConfigureAwait(false);

                    stopwatch.Stop();
                    StructuredLogger.LogJobCompleted(_logger, job.Id, job.DisplayName, stopwatch.Elapsed, 
                        result.Success, result.StdOut);

                    return result;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    StructuredLogger.LogJobFailed(_logger, job.Id, job.DisplayName, ex);
                    throw;
                }
            }
        }

        private bool IsValidFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            try
            {
                var invalidChars = System.IO.Path.GetInvalidPathChars();
                if (path.Any(c => invalidChars.Contains(c)))
                    return false;

                // Detect path traversal attempts (..\ or ../)
                if (path.Contains(".."))
                    return false;

                // Verify the path can be resolved without traversal
                var fullPath = Path.GetFullPath(path);

                // Ensure resolved path hasn't escaped the intended location
                // (this is checked in IsPathSafeForDeletion for deletion operations)
                return !string.IsNullOrWhiteSpace(fullPath);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateFileFilter(string filter, string targetFolder)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true; // null or empty filter defaults to "*"

            // Reject filters that contain path separators or traversal attempts
            if (filter.Contains("/") || filter.Contains("\\") || filter.Contains(".."))
                return false;

            // Reject filters that would delete everything in every subdirectory
            // Patterns like "*" or "*.*" without qualification are dangerous when Recurse=true
            if (filter == "*" || filter == "*.*")
                return false;

            // Allow common safe patterns
            // Examples: "*.log", "*.tmp", "prefix_*.txt", etc.
            try
            {
                // Verify the filter can be used with Directory.GetFiles
                var testPath = Path.Combine(targetFolder, filter);
                return !string.IsNullOrWhiteSpace(testPath);
            }
            catch
            {
                return false;
            }
        }

        private async Task<JobResult> ExecuteOnceAsync(JobConfig job, CancellationToken cancellationToken)
        {
            var result = new JobResult();
            string? tempScriptPath = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (job.Type == Models.JobType.FileCleanup)
                {
                    return ExecuteFileCleanupAsync(job);
                }

                var fileName = job.Command;
                var arguments = job.Arguments ?? string.Empty;

                if (job.Type == Models.JobType.PowerShell)
                {
                    // Prepare a temporary script file and execute with pwsh or powershell
                    tempScriptPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"job_{Guid.NewGuid()}.ps1");
                    await System.IO.File.WriteAllTextAsync(tempScriptPath, job.Script ?? string.Empty, cancellationToken).ConfigureAwait(false);

                    // prefer pwsh (PowerShell Core) if available, otherwise fallback to powershell
                    fileName = string.IsNullOrWhiteSpace(fileName) ? "powershell.exe" : fileName;
                    arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{tempScriptPath}\"";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = job.WorkingDirectory ?? string.Empty,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                var stdOut = new StringBuilder();
                var stdErr = new StringBuilder();

                // Attach an event handler to capture each line in real-time
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) { stdOut.AppendLine(e.Data); } };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) { stdErr.AppendLine(e.Data); } };

                _logger.LogInformation("Process execution initiated | JobId: {JobId} | DisplayName: {DisplayName} | Command: {Command} | Type: {Type}", 
                    job.Id, job.DisplayName, fileName, job.Type);

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    StructuredLogger.LogJobFailed(_logger, job.Id, job.DisplayName, ex, -1);
                    result.Success = false;
                    result.ExitCode = -1;
                    result.StdErr = ex.ToString();
                    return result;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var timeout = job.TimeoutSeconds > 0 ? TimeSpan.FromSeconds(job.TimeoutSeconds) : Timeout.InfiniteTimeSpan;

                var exited = await WaitForExitAsync(process, timeout, cancellationToken).ConfigureAwait(false);

                if (!exited)
                {
                    try
                    {
                        process.Kill(true);
                        _logger.LogInformation("Process force-terminated due to timeout | JobId: {JobId} | ProcessId: {ProcessId}", 
                            job.Id, process.Id);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Process already exited - this is benign
                        _logger.LogDebug(ex, "Process already exited when attempting force-terminate | JobId: {JobId}", job.Id);
                    }
                    catch (Exception ex)
                    {
                        // Unexpected error during termination
                        _logger.LogWarning(ex, "Failed to force-terminate process | JobId: {JobId} | ProcessId: {ProcessId}", 
                            job.Id, process.Id);
                    }
                    result.Success = false;
                    result.ExitCode = -1;
                    result.StdErr = "Timed out" + stdErr.ToString();
                    _logger.LogWarning("Job {JobId} timed out after {TimeoutSeconds}s", job.Id, job.TimeoutSeconds);
                }
                else
                {
                    result.ExitCode = process.ExitCode;
                    result.StdOut = stdOut.ToString();
                    result.StdErr = stdErr.ToString();
                    result.Success = process.ExitCode == 0;

                    StructuredLogger.LogProcessExecution(_logger, job.Id, fileName, arguments, process.ExitCode);
                }

                // Log stdout/stderr at appropriate levels
                if (!string.IsNullOrWhiteSpace(result.StdOut))
                    _logger.LogDebug("Process stdout | JobId: {JobId} | Output: {StdOut}", job.Id, result.StdOut);

                if (!string.IsNullOrWhiteSpace(result.StdErr))
                    _logger.LogWarning("Process stderr | JobId: {JobId} | Error: {StdErr}", job.Id, result.StdErr);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var classification = ErrorClassifier.Classify(ex);
                _logger.LogError(ex, "Job {JobId} execution failed with {Classification}", job.Id, classification);
                result.Success = false;
                result.ExitCode = -1;
                result.StdErr = ex.ToString();
            }
            finally
            {
                stopwatch.Stop();
                if (tempScriptPath is not null)
                {
                    try
                    {
                        System.IO.File.Delete(tempScriptPath);
                        _logger.LogDebug("Temporary script cleaned up | JobId: {JobId} | Path: {TempPath}", 
                            job.Id, tempScriptPath);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        // File already deleted - benign
                        _logger.LogDebug("Temporary script already deleted | JobId: {JobId} | Path: {TempPath}", 
                            job.Id, tempScriptPath);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // Permission denied - may need deferred cleanup
                        _logger.LogWarning(ex, "Cannot delete temporary script (permission denied) | JobId: {JobId} | Path: {TempPath}", 
                            job.Id, tempScriptPath);
                    }
                    catch (Exception ex)
                    {
                        // Unexpected error
                        _logger.LogWarning(ex, "Error deleting temporary script | JobId: {JobId} | Path: {TempPath}", 
                            job.Id, tempScriptPath);
                    }
                }
            }

            return result;
        }

        private bool IsPathSafeForDeletion(string targetFolder)
        {
            try
            {
                // Get the full resolved path to detect any path traversal
                var fullPath = Path.GetFullPath(targetFolder);

                // Check if path is a symbolic link or junction (potential escape route)
                var dirInfo = new DirectoryInfo(fullPath);
                if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    _logger.LogWarning("Target folder is a symbolic link or junction, which is not allowed for safety | Path: {Path}", fullPath);
                    return false;
                }

                // Verify the directory exists after resolution
                if (!Directory.Exists(fullPath))
                {
                    return false;
                }

                // Additional check: ensure the resolved path is still under a reasonable location
                // (not system directories, though this is more of a runtime policy check)
                var systemPaths = new[] { Environment.SystemDirectory, Path.GetPathRoot(Environment.SystemDirectory) };
                foreach (var sysPath in systemPaths)
                {
                    if (fullPath.Equals(sysPath, StringComparison.OrdinalIgnoreCase) || 
                        fullPath.StartsWith(sysPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Target folder is in a system-protected directory, which is not allowed for safety | Path: {Path}", fullPath);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating path safety for deletion | Path: {Path}", targetFolder);
                return false;
            }
        }

        private JobResult ExecuteFileCleanupAsync(JobConfig job)
        {
            var result = new JobResult();
            var stopwatch = Stopwatch.StartNew();

            using (LoggingContext.CreateActivityScope(job.Id, "FileCleanup"))
            {
                try
                {
                    // Validation: Target folder is specified
                    if (string.IsNullOrWhiteSpace(job.TargetFolder))
                    {
                        result.Success = false;
                        result.ExitCode = -1;
                        result.StdErr = "Target folder is not specified";
                        _logger.LogError("FileCleanup validation failed: Target folder not specified for job {JobId}", job.Id);
                        return result;
                    }

                    // Validation: Target folder exists
                    if (!Directory.Exists(job.TargetFolder))
                    {
                        result.Success = false;
                        result.ExitCode = -1;
                        result.StdErr = $"Target folder does not exist: {job.TargetFolder}";
                        _logger.LogError("FileCleanup validation failed: Target folder does not exist | JobId: {JobId} | Path: {Path}", 
                            job.Id, job.TargetFolder);
                        return result;
                    }

                    // Validation: Path is safe for deletion (no symlinks, not system directories, etc.)
                    if (!IsPathSafeForDeletion(job.TargetFolder))
                    {
                        result.Success = false;
                        result.ExitCode = -1;
                        result.StdErr = $"Target folder failed safety validation. It may be a symbolic link, junction, or system directory.";
                        _logger.LogError("FileCleanup validation failed: Path is not safe for deletion | JobId: {JobId} | Path: {Path}", 
                            job.Id, job.TargetFolder);
                        return result;
                    }

                    // Validation: File filter is safe (not overly broad)
                    var filter = string.IsNullOrWhiteSpace(job.FileFilter) ? "*" : job.FileFilter;
                    if (!ValidateFileFilter(filter, job.TargetFolder))
                    {
                        result.Success = false;
                        result.ExitCode = -1;
                        result.StdErr = $"File filter is too broad or invalid: {filter}. Filters like '*' or '*.*' are not allowed.";
                        _logger.LogError("FileCleanup validation failed: File filter is invalid | JobId: {JobId} | Filter: {Filter}", 
                            job.Id, filter);
                        return result;
                    }

                    var searchOption = job.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    var cutoffDate = DateTime.Now.AddDays(-job.FileAgeInDays);

                    _logger.LogInformation("FileCleanup scan initiated | JobId: {JobId} | DisplayName: {DisplayName} | Target: {Target} | Filter: {Filter} | Age: {Age}d | Recurse: {Recurse}",
                        job.Id, job.DisplayName, job.TargetFolder, filter, job.FileAgeInDays, job.Recurse);

                    var filesToDelete = Directory.GetFiles(job.TargetFolder, filter, searchOption)
                        .Where(f => File.GetLastWriteTime(f) <= cutoffDate)
                        .ToList();

                    _logger.LogInformation("FileCleanup scan completed | JobId: {JobId} | FilesFound: {FileCount} | CutoffDate: {CutoffDate}", 
                        job.Id, filesToDelete.Count, cutoffDate);

                    // Pre-deletion audit: Log all files that will be deleted
                    if (filesToDelete.Count > 0)
                    {
                        _logger.LogInformation("FileCleanup: Pre-deletion audit | JobId: {JobId} | Total files to delete: {Count}", 
                            job.Id, filesToDelete.Count);
                    }

                    int deletedCount = 0;
                    var errors = new List<string>();

                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                            _logger.LogDebug("File deleted | JobId: {JobId} | FilePath: {FilePath}", job.Id, file);
                        }
                        catch (Exception ex)
                        {
                            var error = $"Failed to delete {file}: {ex.Message}";
                            errors.Add(error);
                            var classification = ErrorClassifier.Classify(ex);
                            _logger.LogWarning(ex, "File deletion failed | JobId: {JobId} | FilePath: {FilePath} | Classification: {Classification}", 
                                job.Id, file, classification);
                        }
                    }

                    stopwatch.Stop();
                    result.ExitCode = 0;

                    // Improved handling: Distinguish between complete success, partial success, and complete failure
                    if (errors.Count == 0)
                    {
                        // Complete success: All files deleted (or no files to delete)
                        result.Success = true;
                        result.StdOut = $"Successfully deleted {deletedCount} file(s)";
                    }
                    else if (deletedCount > 0 && errors.Count > 0)
                    {
                        // Partial success: Some files deleted, some failed
                        result.Success = false;
                        result.StdOut = $"Partially completed: Successfully deleted {deletedCount} file(s) but {errors.Count} file(s) failed to delete";
                        result.StdErr = $"PARTIAL DELETION - {errors.Count} files could not be deleted:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Take(10))}";
                        if (errors.Count > 10)
                        {
                            result.StdErr += $"{Environment.NewLine}... and {errors.Count - 10} more errors";
                        }
                        _logger.LogWarning("FileCleanup completed with partial success | JobId: {JobId} | Deleted: {Deleted} | Failed: {Failed}", 
                            job.Id, deletedCount, errors.Count);
                    }
                    else
                    {
                        // Complete failure: No files deleted
                        result.Success = false;
                        result.StdOut = $"Failed to delete any files. All {errors.Count} deletion attempt(s) failed.";
                        result.StdErr = string.Join(Environment.NewLine, errors.Take(10));
                        if (errors.Count > 10)
                        {
                            result.StdErr += $"{Environment.NewLine}... and {errors.Count - 10} more errors";
                        }
                    }

                    StructuredLogger.LogFileCleanupOperation(_logger, job.Id, job.TargetFolder, 
                        deletedCount, filter, job.Recurse, stopwatch.Elapsed);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var classification = ErrorClassifier.Classify(ex);
                    _logger.LogError(ex, "FileCleanup job failed | JobId: {JobId} | Classification: {Classification}", 
                        job.Id, classification);
                    result.Success = false;
                    result.ExitCode = -1;
                    result.StdErr = ex.ToString();
                }
            }

            return result;
        }

        private static Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? s, EventArgs e) => tcs.TrySetResult(true);
            process.Exited += Handler;

            if (process.HasExited)
            {
                process.Exited -= Handler;
                return Task.FromResult(true);
            }

            var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

            var delay = timeout == Timeout.InfiniteTimeSpan ? Task.Delay(Timeout.Infinite, cancellationToken) : Task.Delay(timeout, cancellationToken);

            return Task.WhenAny(tcs.Task, delay).ContinueWith(t =>
            {
                registration.Dispose();
                process.Exited -= Handler;
                return tcs.Task.IsCompleted && !t.IsFaulted && !t.IsCanceled;
            }, TaskScheduler.Default);
        }
    }
}
