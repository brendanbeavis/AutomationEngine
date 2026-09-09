using AutomationEngine.Dto;

namespace AutomationEngine.Domain.Extensions;

public static class BlazorUiExtensions
{
    public static string ToLocalDisplay(this DateTime value, string format = "g")
    {
        return value.ToLocalTime().ToString(format);
    }

    public static string ToLocalDisplayOr(this DateTime? value, string fallback = "Never", string format = "g")
    {
        return value.HasValue ? value.Value.ToLocalDisplay(format) : fallback;
    }

    public static string ToResultText(this JobDto job)
    {
        if (job.IsRunning)
            return "Running...";

        if (!job.LastRunSuccess.HasValue)
            return "Never run";

        return job.LastRunSuccess.Value ? "Success" : "Failed";
    }

    public static string ToResultCssClass(this JobDto job)
    {
        if (job.IsRunning)
            return "neon-purple";

        if (!job.LastRunSuccess.HasValue)
            return "neon-orange";

        return job.LastRunSuccess.Value ? "neon-lime" : "neon-pink";
    }

    public static string ToRunButtonTitle(this JobDto job)
    {
        return job.IsRunning ? "Job Running..." : "Run Job Now";
    }

    public static string ToHistoryStatusBadgeClass(this bool success)
    {
        return success ? "bg-success" : "bg-danger";
    }

    public static string ToHistoryStatusText(this bool success)
    {
        return success ? "✓ Success" : "✗ Failed";
    }

    public static string ToModalTitle(this bool isEdit, bool isDupe)
    {
        return isEdit ? "Edit Job" : isDupe ? "Duplicate Job" : "Add Job";
    }

    public static string ToSaveButtonText(this bool isEdit, bool isDupe)
    {
        return isEdit ? "Update" : isDupe ? "Save Copy" : "Add";
    }

    public static string ToToggleEnabledCssClass(this bool isEnabled)
    {
        return isEnabled ? "text-secondary" : "text-success";
    }

    public static string ToToggleEnabledText(this bool isEnabled)
    {
        return isEnabled ? "Disable" : "Enable";
    }

    public static string ToBooleanDisplayText(this bool value)
    {
        return value ? "true" : "false";
    }

    public static string ToBooleanDisplayColor(this bool value)
    {
        return value ? "#39ff14" : "#ff2d95";
    }

    public static string ToTabBadgeClass(this int activeTab, int tabIndex)
    {
        return activeTab == tabIndex ? "bg-cyan-neon" : string.Empty;
    }

    public static string ToThemeCardCssClass(this string? selectedTheme, string theme)
    {
        return selectedTheme == theme ? "theme-card active" : "theme-card";
    }

    public static string ToThemeCardBorderColor(this string? selectedTheme, string theme)
    {
        return selectedTheme == theme ? "var(--cp-accent-cyan)" : "var(--cp-border)";
    }
}
