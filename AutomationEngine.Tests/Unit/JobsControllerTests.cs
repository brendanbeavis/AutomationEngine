using AutomationEngine.Api;
using AutomationEngine.Application.Abstractions;
using AutomationEngine.Application.Abstractions;
using AutomationEngine.Data.Entities;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Services;
using AutomationEngine.Application.Abstractions;
using AutomationEngine.SignalR;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomationEngine.Tests.Unit;

public class JobsControllerTests
{
    [Fact]
    public void GetAllJobs_WithNoJobs_ShouldReturnEmptyList()
    {
        var stateManager = new Mock<IJobStateManager>();
        stateManager.Setup(m => m.GetAllJobs()).Returns(new List<JobEntity>());

        var controller = new JobsController(
            stateManager.Object,
            new Mock<IJobRunner>().Object,
            new Mock<IHubContext<JobStatusHub>>().Object,
            new Mock<ILogger<JobsController>>().Object,
            new Mock<IBackgroundTaskQueue>().Object,
            new Mock<IAuditLogService>().Object,
            new Mock<IJobValidationService>().Object,
            new Mock<IJobStatusService>().Object,
            new Mock<IServiceScopeFactory>().Object);

        var result = controller.GetAllJobs();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var jobs = ok.Value.Should().BeAssignableTo<List<JobDto>>().Subject;
        jobs.Should().BeEmpty();
    }
}

