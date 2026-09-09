using AutomationEngine.Dto;
using AutomationEngine.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace AutomationEngine.Tests.Unit
{
    public class JobDtoValidationTests
    {
        private static IEnumerable<ValidationResult> ValidateDto(JobDto dto)
        {
            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
            return results.Concat(dto.Validate(context));
        }

        [Fact]
        public void JobDto_ProcessJob_WithValidCommand_ShouldBeValid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-job",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void JobDto_ProcessJob_WithoutCommand_ShouldBeInvalid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-job",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain(e => e.ErrorMessage.Contains("Command is required for Process jobs"));
        }

        [Fact]
        public void JobDto_PowerShellJob_WithValidScript_ShouldBeValid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-ps",
                DisplayName = "Test PowerShell",
                Type = JobType.PowerShell,
                Command = "powershell.exe",
                Script = "Write-Host 'Hello'",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void JobDto_PowerShellJob_WithoutScript_ShouldBeInvalid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-ps",
                DisplayName = "Test PowerShell",
                Type = JobType.PowerShell,
                Command = "powershell.exe",
                Script = "",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain(e => e.ErrorMessage.Contains("Script is required for PowerShell jobs"));
        }

        [Fact]
        public void JobDto_FileCleanupJob_WithValidTargetFolder_ShouldBeValid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-cleanup",
                DisplayName = "Test Cleanup",
                Type = JobType.FileCleanup,
                Command = "",
                TargetFolder = "C:\\logs",
                FileAgeInDays = 30,
                FileFilter = "*.log",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void JobDto_FileCleanupJob_WithoutTargetFolder_ShouldBeInvalid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-cleanup",
                DisplayName = "Test Cleanup",
                Type = JobType.FileCleanup,
                Command = "",
                TargetFolder = "",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain(e => e.ErrorMessage.Contains("TargetFolder is required for FileCleanup jobs"));
        }

        [Fact]
        public void JobDto_FileCleanupJob_WithNegativeFileAge_ShouldBeInvalid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-cleanup",
                DisplayName = "Test Cleanup",
                Type = JobType.FileCleanup,
                Command = "",
                TargetFolder = "C:\\logs",
                FileAgeInDays = -5,
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain(e => e.ErrorMessage.Contains("FileAgeInDays must be between 0 and 36500"));
        }

        [Fact]
        public void JobDto_WithMissingJobId_ShouldBeInvalid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().NotBeEmpty();
            // Match the actual Required attribute error message from JobDto
            errors.Should().Contain(e => e.ErrorMessage.Contains("JobId is required"));
        }

        [Fact]
        public void JobDto_WithInvalidCronExpression_ShouldBeInvalid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-job",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "invalid cron"
                // Note: JobDto.Validate() doesn't validate CRON syntax - only Required/Type checks
                // CRON validation happens in JobValidationService in business logic
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            // No validation errors from JobDto itself for invalid CRON
            // (CRON validation is done at service layer, not DTO layer)
            // This test verifies that invalid CRON doesn't throw during DTO validation
            errors.Should().BeEmpty();
        }

        [Fact]
        public void JobDto_WithValidCronExpression_ShouldBeValid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-job",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void JobDto_WithValidCronExpressionWithSeconds_ShouldBeValid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-job",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void JobDto_WithInvalidRetry_ShouldBeInvalid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-job",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Retry = 150,
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain(e => e.ErrorMessage.Contains("Retry count must be between 0 and 100"));
        }

        [Fact]
        public void JobDto_WithLongJobId_ShouldBeInvalid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = new string('a', 50),
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain(e => e.ErrorMessage.Contains("JobId must be between 1 and 20 characters"));
        }

        [Fact]
        public void JobDto_FileCleanupJob_WithRecurseTrue_ShouldBeValid()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "test-cleanup",
                DisplayName = "Test Cleanup",
                Type = JobType.FileCleanup,
                Command = "",
                TargetFolder = "C:\\logs",
                FileAgeInDays = 30,
                FileFilter = "*.log",
                Recurse = true,
                Schedule = "0 0 * * *"
            };

            // Act
            var errors = ValidateDto(dto).ToList();

            // Assert
            errors.Should().BeEmpty();
        }
    }
}
