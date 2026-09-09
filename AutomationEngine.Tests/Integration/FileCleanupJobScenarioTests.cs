using AutomationEngine.Models;
using AutomationEngine.Services;
using AutomationEngine.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AutomationEngine.Tests.Integration
{
    public class FileCleanupJobScenarioTests : IDisposable
    {
        private readonly JobRunner _jobRunner;
        private readonly List<string> _testDirectories = new();

        public FileCleanupJobScenarioTests()
        {
            _jobRunner = TestFixtureHelper.CreateMockJobRunner();
        }

        public void Dispose()
        {
            foreach (var dir in _testDirectories)
            {
                TestFixtureHelper.CleanupTestDirectory(dir);
            }
        }

        private string CreateTrackedTestDirectory(string prefix = "cleanup-test")
        {
            var dir = TestFixtureHelper.CreateTestDirectory(prefix);
            _testDirectories.Add(dir);
            return dir;
        }

        #region Age-Based Cleanup Tests

        [Fact]
        public async Task FileCleanup_RemovesFilesOlderThanSpecifiedAge()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("age-based");

            // Create 5 old files (>10 days old) and 3 new files (<10 days old)
            // Use unique names to avoid overwrites
            for (int i = 0; i < 5; i++)
            {
                var fileName = Path.Combine(testDir, $"old_file_{i}.txt");
                File.WriteAllText(fileName, $"Old content {i}");
                File.SetLastWriteTime(fileName, DateTime.Now.AddDays(-30));
            }

            for (int i = 0; i < 3; i++)
            {
                var fileName = Path.Combine(testDir, $"new_file_{i}.txt");
                File.WriteAllText(fileName, $"New content {i}");
                File.SetLastWriteTime(fileName, DateTime.Now.AddDays(-5));
            }

            var job = new JobConfig
            {
                Id = "age-cleanup",
                DisplayName = "Age-Based Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.txt", // Use specific pattern instead of * (which is rejected for safety)
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("5 file(s)");

        }

        [Fact]
        public async Task FileCleanup_WithZeroAgeDays_RemovesAllFiles()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("zero-age");

            // Create 5 files from today and 3 files from 5 days ago
            // All should be deleted with FileAgeInDays = 0
            for (int i = 0; i < 5; i++)
            {
                var fileName = Path.Combine(testDir, $"today_file_{i}.txt");
                File.WriteAllText(fileName, $"Today {i}");
                File.SetLastWriteTime(fileName, DateTime.Now);
            }

            for (int i = 0; i < 3; i++)
            {
                var fileName = Path.Combine(testDir, $"old_file_{i}.txt");
                File.WriteAllText(fileName, $"Old {i}");
                File.SetLastWriteTime(fileName, DateTime.Now.AddDays(-5));
            }

            var job = new JobConfig
            {
                Id = "zero-age-cleanup",
                DisplayName = "Zero Age Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 0,
                FileFilter = "*.txt", // Use specific pattern instead of * (which is rejected for safety)
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("8 file(s)"); // All files should be deleted
        }

        #endregion

        #region Pattern Matching Tests

        [Fact]
        public async Task FileCleanup_WithSingleExtensionFilter_OnlyDeletesMatchingFiles()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("pattern-match");
            var oldDate = DateTime.Now.AddDays(-15);

            // Create .log files (old)
            for (int i = 0; i < 5; i++)
            {
                var file = Path.Combine(testDir, $"app_{i}.log");
                File.WriteAllText(file, "Log content");
                File.SetLastWriteTime(file, oldDate);
            }

            // Create .txt files (old)
            for (int i = 0; i < 3; i++)
            {
                var file = Path.Combine(testDir, $"readme_{i}.txt");
                File.WriteAllText(file, "Text content");
                File.SetLastWriteTime(file, oldDate);
            }

            var job = new JobConfig
            {
                Id = "log-cleanup",
                DisplayName = "Log File Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("5 file(s)");
            Directory.GetFiles(testDir, "*.log").Length.Should().Be(0);
            Directory.GetFiles(testDir, "*.txt").Length.Should().Be(3);
        }

        [Fact]
        public async Task FileCleanup_WithWildcardPrefix_DeletesMatchingFiles()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("wildcard-prefix");
            var oldDate = DateTime.Now.AddDays(-15);

            // Create temp_*.tmp files (old)
            for (int i = 0; i < 4; i++)
            {
                var file = Path.Combine(testDir, $"temp_{i}.tmp");
                File.WriteAllText(file, "Temp content");
                File.SetLastWriteTime(file, oldDate);
            }

            // Create other files
            var keepFile = Path.Combine(testDir, $"important.tmp");
            File.WriteAllText(keepFile, "Keep this");
            File.SetLastWriteTime(keepFile, DateTime.Now);

            var job = new JobConfig
            {
                Id = "temp-cleanup",
                DisplayName = "Temp File Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "temp_*",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("4 file(s)");
            Directory.GetFiles(testDir, "temp_*").Length.Should().Be(0);
            File.Exists(keepFile).Should().BeTrue();
        }

        #endregion

        #region Recursive Directory Tests

        [Fact]
        public async Task FileCleanup_WithRecurse_DeletesFilesInSubdirectories()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("recursive");
            var oldDate = DateTime.Now.AddDays(-15);

            // Create files in root
            for (int i = 0; i < 3; i++)
            {
                var file = Path.Combine(testDir, $"root_{i}.log");
                File.WriteAllText(file, "Root log");
                File.SetLastWriteTime(file, oldDate);
            }

            // Create subdirectories with files
            for (int d = 1; d <= 3; d++)
            {
                var subDir = Path.Combine(testDir, $"subdir{d}");
                Directory.CreateDirectory(subDir);

                for (int i = 0; i < 2; i++)
                {
                    var file = Path.Combine(subDir, $"sub_{d}_{i}.log");
                    File.WriteAllText(file, "Sub log");
                    File.SetLastWriteTime(file, oldDate);
                }
            }

            var job = new JobConfig
            {
                Id = "recursive-cleanup",
                DisplayName = "Recursive Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.log",
                Recurse = true
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("9 file(s)"); // 3 root + 2*3 subdirs
            Directory.GetFiles(testDir, "*.log", SearchOption.AllDirectories).Length.Should().Be(0);
        }

        [Fact]
        public async Task FileCleanup_WithoutRecurse_IgnoresSubdirectories()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("non-recursive");
            var oldDate = DateTime.Now.AddDays(-15);

            // Create files in root
            for (int i = 0; i < 3; i++)
            {
                var file = Path.Combine(testDir, $"root_{i}.log");
                File.WriteAllText(file, "Root log");
                File.SetLastWriteTime(file, oldDate);
            }

            // Create subdirectories with files
            var subDir = Path.Combine(testDir, "subdir");
            Directory.CreateDirectory(subDir);
            for (int i = 0; i < 2; i++)
            {
                var file = Path.Combine(subDir, $"sub_{i}.log");
                File.WriteAllText(file, "Sub log");
                File.SetLastWriteTime(file, oldDate);
            }

            var job = new JobConfig
            {
                Id = "non-recursive-cleanup",
                DisplayName = "Non-Recursive Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("3 file(s)"); // Only root files
            Directory.GetFiles(testDir, "*.log").Length.Should().Be(0); // Root cleaned
            Directory.GetFiles(Path.Combine(testDir, "subdir"), "*.log").Length.Should().Be(2); // Subdir untouched
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task FileCleanup_WithEmptyDirectory_ShouldSucceed()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("empty");

            var job = new JobConfig
            {
                Id = "empty-cleanup",
                DisplayName = "Empty Directory Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("0 file(s)");
        }

        [Fact]
        public async Task FileCleanup_WithNoMatchingFiles_ShouldSucceed()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("no-match");
            var oldDate = DateTime.Now.AddDays(-15);

            // Create only .txt files
            for (int i = 0; i < 3; i++)
            {
                var file = Path.Combine(testDir, $"file_{i}.txt");
                File.WriteAllText(file, "Text");
                File.SetLastWriteTime(file, oldDate);
            }

            var job = new JobConfig
            {
                Id = "no-match-cleanup",
                DisplayName = "No Match Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.log", // No log files exist
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("0 file(s)");
            Directory.GetFiles(testDir, "*.txt").Length.Should().Be(3); // Files unchanged
        }

        [Fact]
        public async Task FileCleanup_WithMixedAges_DeletesOnlyOldFiles()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("mixed-ages");

            var veryOldDate = DateTime.Now.AddDays(-100);
            var oldDate = DateTime.Now.AddDays(-15);
            var recentDate = DateTime.Now.AddDays(-3);

            // Create very old files
            for (int i = 0; i < 2; i++)
            {
                var file = Path.Combine(testDir, $"very_old_{i}.log");
                File.WriteAllText(file, "Very old");
                File.SetLastWriteTime(file, veryOldDate);
            }

            // Create old files
            for (int i = 0; i < 3; i++)
            {
                var file = Path.Combine(testDir, $"old_{i}.log");
                File.WriteAllText(file, "Old");
                File.SetLastWriteTime(file, oldDate);
            }

            // Create recent files
            for (int i = 0; i < 2; i++)
            {
                var file = Path.Combine(testDir, $"recent_{i}.log");
                File.WriteAllText(file, "Recent");
                File.SetLastWriteTime(file, recentDate);
            }

            var job = new JobConfig
            {
                Id = "mixed-cleanup",
                DisplayName = "Mixed Age Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("5 file(s)"); // 2 very old + 3 old
            Directory.GetFiles(testDir, "*.log").Length.Should().Be(2); // Only recent remain
        }

        [Fact]
        public async Task FileCleanup_WithSpecialCharactersInFilename_DeletesCorrectly()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("special-chars");
            var oldDate = DateTime.Now.AddDays(-15);

            // Create files with special characters
            var specialFiles = new[] 
            { 
                "file-with-dash.log",
                "file_with_underscore.log",
                "file (1).log"
            };

            foreach (var fileName in specialFiles)
            {
                var file = Path.Combine(testDir, fileName);
                File.WriteAllText(file, "Content");
                File.SetLastWriteTime(file, oldDate);
            }

            var job = new JobConfig
            {
                Id = "special-cleanup",
                DisplayName = "Special Characters Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("3 file(s)");
            Directory.GetFiles(testDir, "*.log").Length.Should().Be(0);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task FileCleanup_WithLargeNumberOfFiles_CompletesSuccessfully()
        {
            // Arrange
            var testDir = CreateTrackedTestDirectory("large-cleanup");
            var oldDate = DateTime.Now.AddDays(-15);

            // Create 100 old files
            for (int i = 0; i < 100; i++)
            {
                var file = Path.Combine(testDir, $"file_{i:D3}.log");
                File.WriteAllText(file, $"Content {i}");
                File.SetLastWriteTime(file, oldDate);
            }

            var job = new JobConfig
            {
                Id = "large-cleanup",
                DisplayName = "Large File Cleanup",
                Type = JobType.FileCleanup,
                TargetFolder = testDir,
                FileAgeInDays = 10,
                FileFilter = "*.log",
                Recurse = false
            };

            // Act
            var result = await _jobRunner.RunAsync(job, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StdOut.Should().Contain("100 file(s)");
            Directory.GetFiles(testDir, "*.log").Length.Should().Be(0);
        }

        #endregion
    }
}
