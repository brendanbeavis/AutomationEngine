using AutomationEngine.Application.UseCases.Jobs;
using AutomationEngine.Data.Entities;
using AutomationEngine.Infrastructure.Persistence;
using AutomationEngine.Services;
using AutomationEngine.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AutomationEngine.Tests.Fixtures
{
    /// <summary>
    /// Helper class to set up common test fixtures
    /// </summary>
    public class TestFixtureHelper
    {
        /// <summary>
        /// Creates a mock ILogger for testing
        /// </summary>
        public static Mock<ILogger<T>> CreateMockLogger<T>() where T : class
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Creates a JobStateManager with mocked dependencies
        /// </summary>
        public static JobStateManager CreateMockJobStateManager(AutomationDbContext context)
        {
            var mockLogger = CreateMockLogger<JobStateManager>();
            var mockRepository = new Mock<IJobRepository>();
            var mockCache = new Mock<IJobCache>();
            var mockStateTransition = new Mock<IJobStateTransitionService>();
            var mockRunService = new Mock<IJobRunService>();
            var mockQueryService = new Mock<IJobQueryService>();
            var mockAudit = new Mock<IAuditLogService>();

            // Use callback to defer evaluation until the mock is invoked
            mockRepository
                .Setup(r => r.LoadAllActiveJobsAsync())
                .ReturnsAsync(() => context.Jobs.ToList());

            mockRepository
                .Setup(r => r.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string jobId) => context.Jobs.FirstOrDefault(j => j.JobId == jobId));

            mockRepository
                .Setup(r => r.CreateJobAsync(It.IsAny<JobEntity>()))
                .ReturnsAsync((JobEntity job) =>
                {
                    context.Jobs.Add(job);
                    context.SaveChanges();
                    return job;
                });

            mockRepository
                .Setup(r => r.UpdateJobAsync(It.IsAny<JobEntity>()))
                .ReturnsAsync((JobEntity job) =>
                {
                    context.SaveChanges();
                    return job;
                });

            mockRepository
                .Setup(r => r.DeleteJobAsync(It.IsAny<string>()))
                .Callback((string jobId) =>
                {
                    var job = context.Jobs.FirstOrDefault(j => j.JobId == jobId && j.DeletedAt == null);
                    if (job is not null)
                    {
                        job.DeletedAt = DateTime.UtcNow;
                        context.SaveChanges();
                    }
                })
                .Returns(Task.CompletedTask);

            // Use Callback to defer evaluation
            mockQueryService
                .Setup(q => q.GetAllJobs())
                .Returns(() => context.Jobs.ToList());

            mockQueryService
                .Setup(q => q.GetJobById(It.IsAny<string>()))
                .Returns((string jobId) => context.Jobs.FirstOrDefault(j => j.JobId == jobId));

            return new JobStateManager(
                mockRepository.Object,
                mockCache.Object,
                mockStateTransition.Object,
                mockRunService.Object,
                mockQueryService.Object,
                mockAudit.Object,
                mockLogger.Object);
        }

        /// <summary>
        /// Creates a JobRunner with mocked logger
        /// </summary>
        public static JobRunner CreateMockJobRunner()
        {
            var mockLogger = CreateMockLogger<JobRunner>();
            return new JobRunner(mockLogger.Object);
        }

        /// <summary>
        /// Creates a test directory with optional files
        /// </summary>
        public static string CreateTestDirectory(string? prefix = null)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), prefix ?? "test");
            var dirPath = Path.Combine(tempPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(dirPath);
            return dirPath;
        }

        /// <summary>
        /// Creates test files with specified age
        /// </summary>
        public static List<string> CreateTestFilesWithAge(string directory, int fileCount, int ageInDays)
        {
            var files = new List<string>();
            var targetDate = DateTime.Now.AddDays(-ageInDays);

            for (int i = 0; i < fileCount; i++)
            {
                var fileName = Path.Combine(directory, $"testfile_{i}.log");
                File.WriteAllText(fileName, $"Test content {i}");
                File.SetLastWriteTime(fileName, targetDate);
                files.Add(fileName);
            }

            return files;
        }

        /// <summary>
        /// Cleans up a test directory and all its contents
        /// </summary>
        public static void CleanupTestDirectory(string? directory)
        {
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                try
                {
                    Directory.Delete(directory, recursive: true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }
}

