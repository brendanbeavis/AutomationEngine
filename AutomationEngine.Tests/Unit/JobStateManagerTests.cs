using AutomationEngine.Data.Entities;
using AutomationEngine.Infrastructure.Persistence;
using AutomationEngine.Models;
using AutomationEngine.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AutomationEngine.Tests.Unit
{
    public class JobStateManagerTests : IDisposable
    {
        private readonly AutomationDbContext _context;

        public JobStateManagerTests()
        {
            _context = AutomationDbContextFactory.CreateInMemoryContext();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task LoadJobsAsync_WithEmptyDatabase_ShouldLoadEmpty()
        {
            // Arrange
            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);

            // Act
            await stateManager.LoadJobsAsync();
            var jobs = stateManager.GetAllJobs();

            // Assert
            jobs.Should().BeEmpty();
        }

        [Fact]
        public async Task LoadJobsAsync_WithExistingJobs_ShouldLoadAll()
        {
            // Arrange
            _context.Jobs.Add(new JobEntity
            {
                JobId = "job1",
                DisplayName = "Job 1",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *"
            });
            _context.Jobs.Add(new JobEntity
            {
                JobId = "job2",
                DisplayName = "Job 2",
                Type = JobType.PowerShell,
                Script = "Write-Host 'test'",
                Schedule = "0 0 * * *"
            });
            await _context.SaveChangesAsync();

            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);

            // Act
            await stateManager.LoadJobsAsync();
            var jobs = stateManager.GetAllJobs();

            // Assert
            jobs.Should().HaveCount(2);
            jobs.Should().Contain(j => j.JobId == "job1");
            jobs.Should().Contain(j => j.JobId == "job2");
        }

        [Fact]
        public async Task GetJob_WithExistingJobId_ShouldReturnJob()
        {
            // Arrange
            _context.Jobs.Add(new JobEntity
            {
                JobId = "test-job",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *"
            });
            await _context.SaveChangesAsync();

            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);
            await stateManager.LoadJobsAsync();

            // Act
            var job = stateManager.GetJob("test-job");

            // Assert
            job.Should().NotBeNull();
            job?.JobId.Should().Be("test-job");
            job?.DisplayName.Should().Be("Test Job");
        }

        [Fact]
        public async Task GetJob_WithNonexistentJobId_ShouldReturnNull()
        {
            // Arrange
            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);
            await stateManager.LoadJobsAsync();

            // Act
            var job = stateManager.GetJob("nonexistent");

            // Assert
            job.Should().BeNull();
        }

        [Fact]
        public async Task SaveJobAsync_WithNewJob_ShouldCreateJob()
        {
            // Arrange
            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);

            // Act
            var job = await stateManager.SaveJobAsync(
                jobId: "new-job",
                displayName: "New Job",
                command: "test.exe",
                arguments: "/test",
                workingDirectory: "C:\\temp",
                type: JobType.Process,
                script: null,
                schedule: "0 0 * * *",
                timeoutSeconds: 300,
                retry: 2,
                onFailureNotify: true,
                enabled: true
            );

            // Assert
            job.Should().NotBeNull();
            job.JobId.Should().Be("new-job");
            job.DisplayName.Should().Be("New Job");
            job.Type.Should().Be(JobType.Process);
            job.Enabled.Should().BeTrue();

            var savedJob = _context.Jobs.FirstOrDefault(j => j.JobId == "new-job");
            savedJob.Should().NotBeNull();
        }

        [Fact]
        public async Task SaveJobAsync_WithFileCleanupJob_ShouldSaveFileCleanupProperties()
        {
            // Arrange
            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);

            // Act
            var job = await stateManager.SaveJobAsync(
                jobId: "cleanup-job",
                displayName: "Cleanup Job",
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
                targetFolder: "C:\\logs",
                fileAgeInDays: 30,
                recurse: true,
                fileFilter: "*.log"
            );

            // Assert
            job.Should().NotBeNull();
            job.Type.Should().Be(JobType.FileCleanup);
            job.TargetFolder.Should().Be("C:\\logs");
            job.FileAgeInDays.Should().Be(30);
            job.Recurse.Should().BeTrue();
            job.FileFilter.Should().Be("*.log");

            var savedJob = _context.Jobs.FirstOrDefault(j => j.JobId == "cleanup-job");
            savedJob?.TargetFolder.Should().Be("C:\\logs");
        }

        [Fact]
        public async Task SaveJobAsync_WithExistingJob_ShouldUpdateJob()
        {
            // Arrange
            _context.Jobs.Add(new JobEntity
            {
                JobId = "update-job",
                DisplayName = "Old Name",
                Type = JobType.Process,
                Command = "old.exe",
                Schedule = "0 0 * * *"
            });
            await _context.SaveChangesAsync();

            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);

            // Act
            var job = await stateManager.SaveJobAsync(
                jobId: "update-job",
                displayName: "New Name",
                command: "new.exe",
                arguments: "/new",
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
            job.DisplayName.Should().Be("New Name");
            job.Command.Should().Be("new.exe");
            job.Arguments.Should().Be("/new");
            job.Enabled.Should().BeFalse();

            var savedJob = _context.Jobs.FirstOrDefault(j => j.JobId == "update-job");
            savedJob?.DisplayName.Should().Be("New Name");
        }

        [Fact]
        public async Task DeleteJobAsync_WithExistingJob_ShouldDeleteJob()
        {
            // Arrange
            _context.Jobs.Add(new JobEntity
            {
                JobId = "delete-job",
                DisplayName = "Job to Delete",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *"
            });
            await _context.SaveChangesAsync();

            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);
            await stateManager.LoadJobsAsync();

            // Act
            await stateManager.DeleteJobAsync("delete-job");

            // Assert
            var deletedJob = _context.Jobs.FirstOrDefault(j => j.JobId == "delete-job");
            deletedJob.Should().NotBeNull();
            deletedJob!.DeletedAt.Should().NotBeNull(); // Soft delete: DeletedAt is set
        }

        [Fact]
        public async Task GetAllJobs_ShouldReturnCachedJobs()
        {
            // Arrange
            _context.Jobs.Add(new JobEntity
            {
                JobId = "cached-job",
                DisplayName = "Cached Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *"
            });
            await _context.SaveChangesAsync();

            var stateManager = TestFixtureHelper.CreateMockJobStateManager(_context);
            await stateManager.LoadJobsAsync();

            // Act
            var jobs1 = stateManager.GetAllJobs();
            var jobs2 = stateManager.GetAllJobs();

            // Assert
            jobs1.Should().HaveCount(1);
            jobs2.Should().HaveCount(1);
            jobs1.First().JobId.Should().Be("cached-job");
        }
    }
}
