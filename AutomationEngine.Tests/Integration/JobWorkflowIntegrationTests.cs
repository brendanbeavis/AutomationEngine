using AutomationEngine.Data;
using AutomationEngine.Data.Entities;
using AutomationEngine.Models;
using AutomationEngine.Services;
using AutomationEngine.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AutomationEngine.Tests.Integration
{
    public class JobWorkflowIntegrationTests : IDisposable
    {
        private readonly AutomationDbContext _context;
        private readonly JobStateManager _stateManager;
        private readonly JobRunner _jobRunner;

        public JobWorkflowIntegrationTests()
        {
            _context = AutomationDbContextFactory.CreateInMemoryContext();
            _stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);
            _jobRunner = TestFixtureHelper.CreateMockJobRunner();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task CreateJob_LoadJobs_VerifyPersistence()
        {
            // Arrange
            var jobId = "persist-test";
            var displayName = "Persistence Test Job";

            // Act - Create job
            var createdJob = await _stateManager.SaveJobAsync(
                jobId: jobId,
                displayName: displayName,
                command: "test.exe",
                arguments: null,
                workingDirectory: null,
                type: JobType.Process,
                script: null,
                schedule: "0 0 * * *",
                timeoutSeconds: 0,
                retry: 0,
                onFailureNotify: false,
                enabled: true
            );

            // Act - Load jobs
            await _stateManager.LoadJobsAsync();
            var loadedJob = _stateManager.GetJob(jobId);

            // Assert
            createdJob.Should().NotBeNull();
            loadedJob.Should().NotBeNull();
            loadedJob?.DisplayName.Should().Be(displayName);
            loadedJob?.Type.Should().Be(JobType.Process);
        }

        [Fact]
        public async Task CreateMultipleJobs_VerifyAllPersisted()
        {
            // Arrange
            var jobs = new[]
            {
                new { Id = "job1", Name = "Job 1", Type = JobType.Process },
                new { Id = "job2", Name = "Job 2", Type = JobType.PowerShell },
                new { Id = "job3", Name = "Job 3", Type = JobType.FileCleanup }
            };

            // Act - Create multiple jobs
            foreach (var job in jobs)
            {
                string? targetFolder = job.Type == JobType.FileCleanup ? "C:\\logs" : null;
                await _stateManager.SaveJobAsync(
                    jobId: job.Id,
                    displayName: job.Name,
                    command: job.Type == JobType.FileCleanup ? "" : "test.exe",
                    arguments: null,
                    workingDirectory: null,
                    type: job.Type,
                    script: job.Type == JobType.PowerShell ? "Write-Host 'test'" : null,
                    schedule: "0 0 * * *",
                    timeoutSeconds: 0,
                    retry: 0,
                    onFailureNotify: false,
                    enabled: true,
                    targetFolder: targetFolder
                );
            }

            // Act - Load all jobs
            await _stateManager.LoadJobsAsync();
            var loadedJobs = _stateManager.GetAllJobs();

            // Assert
            loadedJobs.Should().HaveCount(3);
            loadedJobs.Should().Contain(j => j.Type == JobType.Process);
            loadedJobs.Should().Contain(j => j.Type == JobType.PowerShell);
            loadedJobs.Should().Contain(j => j.Type == JobType.FileCleanup);
        }

        [Fact]
        public async Task ExecuteProcessJob_VerifyCompletion()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "integration-process",
                DisplayName = "Integration Process Job",
                Type = JobType.Process,
                Command = "cmd.exe",
                Arguments = "/c exit 0"
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.ExitCode.Should().Be(0);
        }

        [Fact]
        public async Task ExecutePowerShellJob_VerifyCompletion()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "integration-ps",
                DisplayName = "Integration PowerShell Job",
                Type = JobType.PowerShell,
                Script = "Write-Output 'Integration Test'; exit 0"
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("Integration Test");
        }

        [Fact]
        public async Task UpdateJob_VerifyChanges()
        {
            // Arrange
            var jobId = "update-test";

            // Act - Create job
            await _stateManager.SaveJobAsync(
                jobId: jobId,
                displayName: "Original Name",
                command: "original.exe",
                arguments: null,
                workingDirectory: null,
                type: JobType.Process,
                script: null,
                schedule: "0 0 * * *",
                timeoutSeconds: 0,
                retry: 0,
                onFailureNotify: false,
                enabled: true
            );

            // Act - Update job
            var updatedJob = await _stateManager.SaveJobAsync(
                jobId: jobId,
                displayName: "Updated Name",
                command: "updated.exe",
                arguments: "/updated",
                workingDirectory: null,
                type: JobType.Process,
                script: null,
                schedule: "0 12 * * *",
                timeoutSeconds: 600,
                retry: 3,
                onFailureNotify: true,
                enabled: false
            );

            // Assert
            updatedJob.DisplayName.Should().Be("Updated Name");
            updatedJob.Command.Should().Be("updated.exe");
            updatedJob.Arguments.Should().Be("/updated");
            updatedJob.Enabled.Should().BeFalse();

            var fromDb = _context.Jobs.FirstOrDefault(j => j.JobId == jobId);
            fromDb?.DisplayName.Should().Be("Updated Name");
            fromDb?.TimeoutSeconds.Should().Be(600);
        }

        [Fact]
        public async Task DeleteJob_VerifyRemoval()
        {
            // Arrange
            var jobId = "delete-test";

            // Act - Create job
            await _stateManager.SaveJobAsync(
                jobId: jobId,
                displayName: "To Delete",
                command: "test.exe",
                arguments: null,
                workingDirectory: null,
                type: JobType.Process,
                script: null,
                schedule: "0 0 * * *",
                timeoutSeconds: 0,
                retry: 0,
                onFailureNotify: false,
                enabled: true
            );

            var beforeDelete = _context.Jobs.FirstOrDefault(j => j.JobId == jobId);
            beforeDelete.Should().NotBeNull();

            // Act - Delete job
            await _stateManager.DeleteJobAsync(jobId);

            // Assert
            var afterDelete = _context.Jobs.FirstOrDefault(j => j.JobId == jobId);
            afterDelete.Should().BeNull();
        }

        [Fact]
        public async Task FileCleanupJob_EndToEnd_VerifyPersistenceAndExecution()
        {
            // Arrange
            var testDirectory = TestFixtureHelper.CreateTestDirectory("integration-cleanup");
            try
            {
                TestFixtureHelper.CreateTestFilesWithAge(testDirectory, 5, 10);

                var jobId = "integration-cleanup";

                // Act - Create and save FileCleanup job
                var savedJob = await _stateManager.SaveJobAsync(
                    jobId: jobId,
                    displayName: "Integration Cleanup Job",
                    command: "",
                    arguments: null,
                    workingDirectory: null,
                    type: JobType.FileCleanup,
                    script: null,
                    schedule: "0 0 * * *",
                    timeoutSeconds: 0,
                    retry: 0,
                    onFailureNotify: false,
                    enabled: true,
                    targetFolder: testDirectory,
                    fileAgeInDays: 5,
                    recurse: false,
                    fileFilter: "*.log"
                );

                // Assert persistence
                savedJob.Type.Should().Be(JobType.FileCleanup);
                savedJob.TargetFolder.Should().Be(testDirectory);
                savedJob.FileAgeInDays.Should().Be(5);
                savedJob.FileFilter.Should().Be("*.log");

                // Load into JobConfig format for execution
                var jobConfig = new JobConfig
                {
                    Id = jobId,
                    DisplayName = savedJob.DisplayName,
                    Type = savedJob.Type,
                    Command = savedJob.Command,
                    TargetFolder = savedJob.TargetFolder,
                    FileAgeInDays = savedJob.FileAgeInDays,
                    Recurse = savedJob.Recurse,
                    FileFilter = savedJob.FileFilter
                };

                // Act - Execute FileCleanup job
                var result = await _jobRunner.RunAsync(jobConfig, CancellationToken.None);

                // Assert execution
                result.Should().NotBeNull();
                result.Success.Should().BeTrue();
                result.StdOut.Should().Contain("5 file(s)");
                Directory.GetFiles(testDirectory, "*.log").Length.Should().Be(0);
            }
            finally
            {
                TestFixtureHelper.CleanupTestDirectory(testDirectory);
            }
        }

        [Fact]
        public async Task ConcurrentJobOperations_VerifyConsistency()
        {
            // Arrange
            var tasks = new List<Task>();

            // Act - Create jobs concurrently
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                var task = _stateManager.SaveJobAsync(
                    jobId: $"concurrent-job-{index}",
                    displayName: $"Concurrent Job {index}",
                    command: "test.exe",
                    arguments: null,
                    workingDirectory: null,
                    type: JobType.Process,
                    script: null,
                    schedule: "0 0 * * *",
                    timeoutSeconds: 0,
                    retry: 0,
                    onFailureNotify: false,
                    enabled: true
                );
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Act - Load and verify
            await _stateManager.LoadJobsAsync();
            var jobs = _stateManager.GetAllJobs();

            // Assert
            jobs.Should().HaveCount(10);
            for (int i = 0; i < 10; i++)
            {
                jobs.Should().Contain(j => j.JobId == $"concurrent-job-{i}");
            }
        }
    }
}
