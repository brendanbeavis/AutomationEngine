using System.Text;

namespace AutomationEngine.Extensions;

public static class StringExt
{
    public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "…")
    {
        return value?.Length > maxLength
            ? value.Substring(0, maxLength) + truncationSuffix
            : value;
    }
}

public static class IntegerExtensions
{
    public static string MsToString(this int integer)
    {
        if (integer <= 0)
            return "-";

        try
        {
            var duration = TimeSpan.FromMilliseconds(integer);

            return duration.TotalSeconds < 1 ? $"{Math.Max(duration.TotalSeconds, 0.01):0.00}sec"
                 : duration.TotalMinutes < 1 ? $"{duration.TotalSeconds:0.0}sec"
                 : duration.TotalMinutes < 10 ? $"{(int)duration.TotalMinutes}min {duration.Seconds:00}sec"
                 : duration.TotalMinutes < 60 ? $"{(int)duration.TotalMinutes:00}min {duration.Seconds:00}sec"
                 : $"{(int)duration.TotalHours:00}hrs {duration.TotalMinutes:00}min";
        }
        catch (OverflowException ex)
        {
            // TimeSpan range exceeded - return descriptive indicator
            System.Diagnostics.Debug.WriteLine($"Duration overflow formatting failed for {integer}ms: {ex.Message}");
            return ">>>"; // Indicates overflow, not generic error
        }
        catch (Exception ex)
        {
            // Unexpected formatting error - return indicator and trace for investigation
            System.Diagnostics.Debug.WriteLine($"Duration format error for {integer}ms: {ex}");
            return "?";
        }
    }
}
