using AutomationEngine.Api;
using AutomationEngine.Data.Entities;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Services;
using AutomationEngine.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AutomationEngine.Tests.Unit
{
    public class JobsControllerTests
    {
        private readonly Mock<JobStateManager> _mockStateManager;
        private readonly Mock<JobRunner> _mockRunner;
        private readonly JobsController _controller;

        public JobsControllerTests()
        {
            _mockStateManager = new Mock<JobStateManager>(
                new Mock<IServiceProvider>().Object,
                TestFixtureHelper.CreateMockLogger<JobStateManager>().Object);
            _mockRunner = new Mock<JobRunner>(
                TestFixtureHelper.CreateMockLogger<JobRunner>().Object);
            _controller = new JobsController(_mockStateManager.Object, _mockRunner.Object, 
                TestFixtureHelper.CreateMockLogger<JobsController>().Object);
        }

        #region GetAllJobs Tests

        [Fact]
        public void GetAllJobs_WithNoJobs_ShouldReturnEmptyList()
        {
            // Arrange
            _mockStateManager.Setup(m => m.GetAllJobs()).Returns(new List<JobEntity>());

            // Act
            var result = _controller.GetAllJobs();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jobs = okResult.Value.Should().BeAssignableTo<List<JobDto>>().Subject;
            jobs.Should().BeEmpty();
        }

        [Fact]
        public void GetAllJobs_WithExistingJobs_ShouldReturnAllJobs()
        {
            // Arrange
            var jobEntities = new List<JobEntity>
            {
                new JobEntity 
                { 
                    JobId = "job1", 
                    DisplayName = "Job 1", 
                    Type = JobType.Process, 
                    Command = "test.exe",
                    Schedule = "0 0 * * *",
                    Runs = new List<JobRunEntity>()
                },
                new JobEntity 
                { 
                    JobId = "job2", 
                    DisplayName = "Job 2", 
                    Type = JobType.PowerShell, 
                    Script = "Write-Host 'test'",
                    Schedule = "0 0 * * *",
                    Runs = new List<JobRunEntity>()
                }
            };
            _mockStateManager.Setup(m => m.GetAllJobs()).Returns(jobEntities);

            // Act
            var result = _controller.GetAllJobs();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var jobs = okResult.Value.Should().BeAssignableTo<List<JobDto>>().Subject;
            jobs.Should().HaveCount(2);
        }

        #endregion

        #region GetJob Tests

        [Fact]
        public void GetJob_WithExistingJobId_ShouldReturnJob()
        {
            // Arrange
            var jobEntity = new JobEntity
            {
                JobId = "test-job",
                DisplayName = "Test Job",
                Type = JobType.Process,
                Command = "test.exe",
                Schedule = "0 0 * * *",
                Runs = new List<JobRunEntity>()
            };
            _mockStateManager.Setup(m => m.GetJob("test-job")).Returns(jobEntity);

            // Act
            var result = _controller.GetJob("test-job");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var job = okResult.Value.Should().BeAssignableTo<JobDto>().Subject;
            job.JobId.Should().Be("test-job");
        }

        [Fact]
        public void GetJob_WithNonexistentJobId_ShouldReturnNotFound()
        {
            // Arrange
            _mockStateManager.Setup(m => m.GetJob("nonexistent")).Returns((JobEntity?)null);

            // Act
            var result = _controller.GetJob("nonexistent");

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region CreateOrUpdateJob Tests

        [Fact]
        public async Task CreateOrUpdateJob_WithValidProcessJob_ShouldSucceed()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "new-job",
                DisplayName = "New Job",
                Type = JobType.Process,
                Command = "test.exe",
                Arguments = "/test",
                Schedule = "0 0 * * *"
            };

            var savedEntity = new JobEntity
            {
                JobId = "new-job",
                DisplayName = "New Job",
                Type = JobType.Process,
                Command = "test.exe",
                Arguments = "/test",
                Schedule = "0 0 * * *",
                Runs = new List<JobRunEntity>()
            };
            _mockStateManager.Setup(m => m.SaveJobAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<JobType>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>()
            )).ReturnsAsync(savedEntity);

            // Act
            var result = await _controller.CreateOrUpdateJob(dto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDto = okResult.Value.Should().BeAssignableTo<JobDto>().Subject;
            returnedDto.JobId.Should().Be("new-job");
        }

        [Fact]
        public async Task CreateOrUpdateJob_WithMissingJobId_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "",
                DisplayName = "New Job",
                Type = JobType.Process,
                Command = "test.exe"
            };

            // Act
            var result = await _controller.CreateOrUpdateJob(dto);

            // Assert
            var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badResult.Value.Should().Be("JobId is required");
        }

        [Fact]
        public async Task CreateOrUpdateJob_ProcessJobWithoutCommand_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "new-job",
                DisplayName = "New Job",
                Type = JobType.Process,
                Command = ""
            };

            // Act
            var result = await _controller.CreateOrUpdateJob(dto);

            // Assert
            var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badResult.Value.Should().Be("Command is required for Process jobs");
        }

        [Fact]
        public async Task CreateOrUpdateJob_PowerShellJobWithoutScript_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "new-job",
                DisplayName = "New Job",
                Type = JobType.PowerShell,
                Command = "powershell.exe",
                Script = ""
            };

            // Act
            var result = await _controller.CreateOrUpdateJob(dto);

            // Assert
            var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badResult.Value.Should().Be("Script is required for PowerShell jobs");
        }

        [Fact]
        public async Task CreateOrUpdateJob_FileCleanupJobWithoutTargetFolder_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "cleanup-job",
                DisplayName = "Cleanup Job",
                Type = JobType.FileCleanup,
                Command = "",
                TargetFolder = ""
            };

            // Act
            var result = await _controller.CreateOrUpdateJob(dto);

            // Assert
            var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badResult.Value.Should().Be("Target Folder is required for FileCleanup jobs");
        }

        [Fact]
        public async Task CreateOrUpdateJob_FileCleanupJobWithValidTargetFolder_ShouldSucceed()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "cleanup-job",
                DisplayName = "Cleanup Job",
                Type = JobType.FileCleanup,
                Command = "",
                TargetFolder = "C:\\logs",
                FileAgeInDays = 30,
                FileFilter = "*.log",
                Schedule = "0 0 * * *"
            };

            var savedEntity = new JobEntity
            {
                JobId = "cleanup-job",
                DisplayName = "Cleanup Job",
                Type = JobType.FileCleanup,
                TargetFolder = "C:\\logs",
                FileAgeInDays = 30,
                FileFilter = "*.log",
                Schedule = "0 0 * * *",
                Runs = new List<JobRunEntity>()
            };
            _mockStateManager.Setup(m => m.SaveJobAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<JobType>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>()
            )).ReturnsAsync(savedEntity);

            // Act
            var result = await _controller.CreateOrUpdateJob(dto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDto = okResult.Value.Should().BeAssignableTo<JobDto>().Subject;
            returnedDto.JobId.Should().Be("cleanup-job");
            returnedDto.TargetFolder.Should().Be("C:\\logs");
        }

        [Fact]
        public async Task CreateOrUpdateJob_WithSaveException_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new JobDto
            {
                JobId = "error-job",
                DisplayName = "Error Job",
                Type = JobType.Process,
                Command = "test.exe"
            };

            _mockStateManager.Setup(m => m.SaveJobAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<JobType>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>()
            )).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateOrUpdateJob(dto);

            // Assert
            var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badResult.Value.Should().Be("Database error");
        }

        #endregion

        #region DeleteJob Tests

        [Fact]
        public async Task DeleteJob_WithExistingJobId_ShouldReturnNoContent()
        {
            // Arrange
            _mockStateManager.Setup(m => m.DeleteJobAsync("existing-job"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteJob("existing-job");

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mockStateManager.Verify(m => m.DeleteJobAsync("existing-job"), Times.Once);
        }

        #endregion
    }
}
