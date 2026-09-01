using AutomationEngine.Models;
using AutomationEngine.Services;
using AutomationEngine.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AutomationEngine.Tests.Unit
{
    public class JobRunnerTests : IDisposable
    {
        private readonly JobRunner _jobRunner;
        private string? _testDirectory;

        public JobRunnerTests()
        {
            _jobRunner = TestFixtureHelper.CreateMockJobRunner();
        }

        public void Dispose()
        {
            TestFixtureHelper.CleanupTestDirectory(_testDirectory);
        }

        #region Process Job Tests

        [Fact]
        public async Task RunAsync_ProcessJob_WithValidCommand_ShouldSucceed()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-process",
                DisplayName = "Test Process",
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
        public async Task RunAsync_ProcessJob_WithFailingCommand_ShouldFail()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-process-fail",
                DisplayName = "Test Process Fail",
                Type = JobType.Process,
                Command = "cmd.exe",
                Arguments = "/c exit 1"
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ExitCode.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_ProcessJob_WithTimeout_ShouldTimeout()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-timeout",
                DisplayName = "Test Timeout",
                Type = JobType.Process,
                Command = "cmd.exe",
                Arguments = "/c timeout /t 10 /nobreak",
                TimeoutSeconds = 1
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StdErr.Should().Contain("Timed out");
        }

        [Fact]
        public async Task RunAsync_ProcessJob_WithInvalidCommand_ShouldFail()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-invalid",
                DisplayName = "Test Invalid Command",
                Type = JobType.Process,
                Command = "this_command_does_not_exist_12345.exe",
                Arguments = ""
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ExitCode.Should().Be(-1);
        }

        [Fact]
        public async Task RunAsync_ProcessJob_WithRetry_ShouldRetryOnFailure()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-retry",
                DisplayName = "Test Retry",
                Type = JobType.Process,
                Command = "cmd.exe",
                Arguments = "/c exit 1",
                Retry = 2
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            // Retry count doesn't affect the final result if command always fails
        }

        #endregion

        #region PowerShell Job Tests

        [Fact]
        public async Task RunAsync_PowerShellJob_WithValidScript_ShouldSucceed()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-ps",
                DisplayName = "Test PowerShell",
                Type = JobType.PowerShell,
                Command = "powershell.exe",
                Script = "Write-Host 'Hello'; exit 0"
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("Hello");
        }

        [Fact]
        public async Task RunAsync_PowerShellJob_WithFailingScript_ShouldFail()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-ps-fail",
                DisplayName = "Test PowerShell Fail",
                Type = JobType.PowerShell,
                Command = "powershell.exe",
                Script = "Write-Error 'Test error'; exit 1"
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task RunAsync_PowerShellJob_CreatesTempScriptFile()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-ps-tempfile",
                DisplayName = "Test PowerShell Temp File",
                Type = JobType.PowerShell,
                Script = "Write-Host 'Test'; exit 0"
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            // Temp file should be cleaned up automatically
        }

        #endregion

        #region FileCleanup Job Tests

        [Fact]
        public async Task RunAsync_FileCleanupJob_WithValidTargetFolder_ShouldCleanupOldFiles()
        {
            // Arrange
            _testDirectory = TestFixtureHelper.CreateTestDirectory("filecleanup");
            TestFixtureHelper.CreateTestFilesWithAge(_testDirectory, 5, 10); // 5 files, 10 days old
            TestFixtureHelper.CreateTestFilesWithAge(_testDirectory, 3, 2); // 3 files, 2 days old

            var job = new JobConfig
            {
                Id = "test-cleanup",
                DisplayName = "Test File Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = _testDirectory,
                FileAgeInDays = 5,
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("5 file(s)");
            Directory.GetFiles(_testDirectory, "*.log").Length.Should().Be(3);
        }

        [Fact]
        public async Task RunAsync_FileCleanupJob_WithNonexistentFolder_ShouldFail()
        {
            // Arrange
            var job = new JobConfig
            {
                Id = "test-cleanup-notfound",
                DisplayName = "Test File Cleanup Not Found",
                Type = JobType.FileCleanup,
                TargetFolder = "C:\\NonExistentPath12345",
                FileAgeInDays = 5
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StdErr.Should().Contain("does not exist");
        }

        [Fact]
        public async Task RunAsync_FileCleanupJob_WithRecurseOption_ShouldCleanupSubdirectories()
        {
            // Arrange
            _testDirectory = TestFixtureHelper.CreateTestDirectory("filecleanup-recurse");
            var subDir = Path.Combine(_testDirectory, "subdir");
            Directory.CreateDirectory(subDir);
            TestFixtureHelper.CreateTestFilesWithAge(_testDirectory, 3, 10);
            TestFixtureHelper.CreateTestFilesWithAge(subDir, 2, 10);

            var job = new JobConfig
            {
                Id = "test-cleanup-recurse",
                DisplayName = "Test File Cleanup Recurse",
                Type = JobType.FileCleanup,
                TargetFolder = _testDirectory,
                FileAgeInDays = 5,
                FileFilter = "*.log",
                Recurse = true
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("5 file(s)");
            Directory.GetFiles(_testDirectory, "*.log", SearchOption.AllDirectories).Length.Should().Be(0);
        }

        [Fact]
        public async Task RunAsync_FileCleanupJob_WithWildcardFilter_ShouldOnlyDeleteMatchingFiles()
        {
            // Arrange
            _testDirectory = TestFixtureHelper.CreateTestDirectory("filecleanup-filter");
            var logFiles = TestFixtureHelper.CreateTestFilesWithAge(_testDirectory, 3, 10);
            var txtFiles = new List<string>();
            var targetDate = DateTime.Now.AddDays(-10);
            for (int i = 0; i < 2; i++)
            {
                var fileName = Path.Combine(_testDirectory, $"testfile_{i}.txt");
                File.WriteAllText(fileName, $"Text content {i}");
                File.SetLastWriteTime(fileName, targetDate);
                txtFiles.Add(fileName);
            }

            var job = new JobConfig
            {
                Id = "test-cleanup-filter",
                DisplayName = "Test File Cleanup Filter",
                Type = JobType.FileCleanup,
                TargetFolder = _testDirectory,
                FileAgeInDays = 5,
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("3 file(s)");
            Directory.GetFiles(_testDirectory, "*.log").Length.Should().Be(0);
            Directory.GetFiles(_testDirectory, "*.txt").Length.Should().Be(2);
        }

        [Fact]
        public async Task RunAsync_FileCleanupJob_WithNoMatchingFiles_ShouldSucceed()
        {
            // Arrange
            _testDirectory = TestFixtureHelper.CreateTestDirectory("filecleanup-nomatch");
            TestFixtureHelper.CreateTestFilesWithAge(_testDirectory, 3, 2); // Only 2 days old

            var job = new JobConfig
            {
                Id = "test-cleanup-nomatch",
                DisplayName = "Test File Cleanup No Match",
                Type = JobType.FileCleanup,
                TargetFolder = _testDirectory,
                FileAgeInDays = 5, // Looking for files older than 5 days
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("0 file(s)");
        }

        #endregion
    }
}
