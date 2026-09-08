using AutomationEngine.Domain.Extensions;
using AutomationEngine.Dto;
using FluentAssertions;
using Xunit;

namespace AutomationEngine.Tests.Unit;

public class BlazorUiExtensionsTests
{
    [Fact]
    public void ToLocalDisplayOr_WithNullValue_ReturnsFallback()
    {
        DateTime? value = null;

        var formatted = value.ToLocalDisplayOr("N/A");

        formatted.Should().Be("N/A");
    }

    [Fact]
    public void ToResultText_And_ToResultCssClass_WithSuccessfulJob_ReturnSuccessValues()
    {
        var job = new JobDto { IsRunning = false, LastRunSuccess = true };

        var resultText = job.ToResultText();
        var cssClass = job.ToResultCssClass();

        resultText.Should().Be("Success");
        cssClass.Should().Be("neon-lime");
    }

    [Fact]
    public void ToRunButtonTitle_WithRunningJob_ReturnsRunningTitle()
    {
        var job = new JobDto { IsRunning = true };

        var title = job.ToRunButtonTitle();

        title.Should().Be("Job Running...");
    }

    [Fact]
    public void HistoryStatusExtensions_WithFailedRun_ReturnExpectedValues()
    {
        var cssClass = false.ToHistoryStatusBadgeClass();
        var text = false.ToHistoryStatusText();

        cssClass.Should().Be("bg-danger");
        text.Should().Be("✗ Failed");
    }

    [Fact]
    public void ToModalTitle_WhenEdit_ReturnsEditJob()
    {
        var title = true.ToModalTitle(false);

        title.Should().Be("Edit Job");
    }

    [Fact]
    public void ToSaveButtonText_WhenDuplicate_ReturnsSaveCopy()
    {
        var text = false.ToSaveButtonText(true);

        text.Should().Be("Save Copy");
    }

    [Fact]
    public void ToggleEnabledExtensions_WhenDisabled_ReturnEnableAndSuccessClass()
    {
        var cssClass = false.ToToggleEnabledCssClass();
        var text = false.ToToggleEnabledText();

        cssClass.Should().Be("text-success");
        text.Should().Be("Enable");
    }

    [Fact]
    public void BooleanDisplayExtensions_WhenTrue_ReturnTrueAndGreen()
    {
        var text = true.ToBooleanDisplayText();
        var color = true.ToBooleanDisplayColor();

        text.Should().Be("true");
        color.Should().Be("#39ff14");
    }

    [Fact]
    public void SettingsTabAndThemeExtensions_WhenSelected_ReturnActiveValues()
    {
        var tabClass = 1.ToTabBadgeClass(1);
        var cardClass = "dark".ToThemeCardCssClass("dark");
        var borderColor = "dark".ToThemeCardBorderColor("dark");

        tabClass.Should().Be("bg-cyan-neon");
        cardClass.Should().Be("theme-card active");
        borderColor.Should().Be("var(--cp-accent-cyan)");
    }
}
