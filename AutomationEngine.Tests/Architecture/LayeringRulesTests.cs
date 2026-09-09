using AutomationEngine.Api;
using AutomationEngine.Application.Abstractions;
using AutomationEngine.Application.UseCases.Jobs;
using AutomationEngine.Services;
using AutomationEngine.Application.Abstractions;
using FluentAssertions;
using Xunit;

namespace AutomationEngine.Tests.Architecture;

public class LayeringRulesTests
{
    [Fact]
    public void JobsController_ShouldDependOnAbstractions_ForStateManagerAndRunner()
    {
        var ctor = typeof(JobsController).GetConstructors().Single();
        var parameterTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();

        parameterTypes.Should().Contain(typeof(IJobStateManager));
        parameterTypes.Should().Contain(typeof(IJobRunner));
        parameterTypes.Should().NotContain(typeof(JobStateManager));
        parameterTypes.Should().NotContain(typeof(JobRunner));
    }

    [Fact]
    public void JobStateManager_ShouldImplement_IJobStateManager()
    {
        typeof(IJobStateManager).IsAssignableFrom(typeof(JobStateManager)).Should().BeTrue();
    }
}

