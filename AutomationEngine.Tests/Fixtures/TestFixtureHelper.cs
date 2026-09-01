using AutomationEngine.Data;
using AutomationEngine.Services;
using Microsoft.Extensions.DependencyInjection;
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
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockLogger = CreateMockLogger<JobStateManager>();

            // Setup service provider to return our context
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(AutomationDbContext)))
                .Returns(context);

            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(ILogger<JobStateManager>)))
                .Returns(mockLogger.Object);

            // Setup CreateScope
            var mockScope = new Mock<IServiceScope>();
            mockScope
                .Setup(s => s.ServiceProvider.GetService(typeof(AutomationDbContext)))
                .Returns(context);

            mockServiceProvider
                .Setup(sp => sp.CreateScope())
                .Returns(mockScope.Object);

            return new JobStateManager(mockServiceProvider.Object, mockLogger.Object);
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
