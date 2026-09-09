using AutomationEngine.Models.Cron;
using AutomationEngine.Utilities.Cron;
using Cronos;
using Microsoft.AspNetCore.Components;

namespace AutomationEngine.Components;

public partial class CronScheduleBuilder : ComponentBase
{
    [Parameter]
    public CronSchedule Schedule { get; set; } = new();

    [Parameter]
    public EventCallback<CronSchedule> ScheduleChanged { get; set; }

    [Parameter]
    public bool ShowPreview { get; set; } = true;

    [Parameter]
    public bool ShowCustom { get; set; } = true;

    [Parameter]
    public int NextRunCount { get; set; } = 5;

    [Parameter]
    public TimeZoneInfo TimeZone { get; set; } =
        TimeZoneInfo.Local;


    protected string Expression { get; private set; } = string.Empty;

    protected string Description { get; private set; } = string.Empty;

    protected string? ErrorMessage { get; private set; }

    protected bool IsValid =>
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        !string.IsNullOrWhiteSpace(Expression);

    protected List<DateTime> NextOccurrences { get; } = [];


    protected IReadOnlyList<DayOfWeek> Days { get; } =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];


    protected override void OnInitialized()
    {
        Update();
    }


    protected async Task OnTypeChanged(ChangeEventArgs args)
    {
        if (Enum.TryParse<CronScheduleType>(
                args.Value?.ToString(),
                out var type))
        {
            Schedule.ScheduleType = type;

            SetDefaults(type);

            await NotifyScheduleChanged();
        }
    }


    private void SetDefaults(CronScheduleType type)
    {
        switch (type)
        {
            case CronScheduleType.MinuteInterval:
                Schedule.MinuteInterval = 5;
                break;

            case CronScheduleType.Hourly:
                Schedule.Minute = 0;
                break;

            case CronScheduleType.HourlyInterval:
                Schedule.HourInterval = 1;
                Schedule.Minute = 0;
                break;

            case CronScheduleType.Daily:
                Schedule.Hour = 0;
                Schedule.Minute = 0;
                break;

            case CronScheduleType.Weekly:

                if (Schedule.DaysOfWeek.Count == 0)
                {
                    Schedule.DaysOfWeek =
                    [
                        DayOfWeek.Monday
                    ];
                }

                break;

            case CronScheduleType.Monthly:
                Schedule.DayOfMonth = 1;
                Schedule.Hour = 0;
                Schedule.Minute = 0;
                break;
        }
    }


    protected async Task ToggleDay(
        DayOfWeek day,
        ChangeEventArgs args)
    {
        var selected =
            args.Value is bool value && value;

        if (selected)
        {
            Schedule.DaysOfWeek.Add(day);
        }
        else
        {
            Schedule.DaysOfWeek.Remove(day);
        }

        await NotifyScheduleChanged();
    }


    protected async Task NotifyScheduleChanged()
    {
        Update();

        if (ScheduleChanged.HasDelegate)
        {
            await ScheduleChanged.InvokeAsync(Schedule);
        }
    }


    private void Update()
    {
        ErrorMessage = null;
        Expression = string.Empty;
        Description = string.Empty;

        NextOccurrences.Clear();

        try
        {
            Expression =
                CronExpressionGenerator.Generate(Schedule);

            var cron = CronExpression.Parse(Expression);

            Description =
                BuildDescription();

            if (ShowPreview)
            {
                CalculateNextOccurrences(cron);
            }
        }
        catch (Exception ex) when (
            ex is CronFormatException ||
            ex is ArgumentException ||
            ex is ArgumentOutOfRangeException)
        {
            ErrorMessage = ex.Message;
        }
    }


    private void CalculateNextOccurrences(
        CronExpression cron)
    {
        var now = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZone);

        var occurrence = cron.GetNextOccurrence(
            now,
            TimeZone);

        while (
            occurrence.HasValue &&
            NextOccurrences.Count < NextRunCount)
        {
            NextOccurrences.Add(
                occurrence.Value.DateTime);

            occurrence = cron.GetNextOccurrence(
                occurrence.Value,
                TimeZone);
        }
    }


    private string BuildDescription()
    {
        return Schedule.ScheduleType switch
        {
            CronScheduleType.EveryMinute =>
                "Every minute",

            CronScheduleType.MinuteInterval =>
                $"Every {Schedule.MinuteInterval} minutes",

            CronScheduleType.Hourly =>
                $"Every hour at minute {Schedule.Minute}",

            CronScheduleType.HourlyInterval =>
                $"Every {Schedule.HourInterval} hours at minute {Schedule.Minute}",

            CronScheduleType.Daily =>
                $"Every day at {Schedule.Hour:00}:{Schedule.Minute:00}",

            CronScheduleType.Weekly =>
                BuildWeeklyDescription(),

            CronScheduleType.Monthly =>
                $"Monthly on day {Schedule.DayOfMonth} at {Schedule.Hour:00}:{Schedule.Minute:00}",

            CronScheduleType.Custom =>
                "Custom cron schedule",

            _ => Expression
        };
    }


    private string BuildWeeklyDescription()
    {
        if (Schedule.DaysOfWeek.Count == 0)
            return "No days selected";

        var days = Schedule.DaysOfWeek
            .OrderBy(d => (int)d)
            .Select(GetDayName);

        return
            $"Every {string.Join(", ", days)} " +
            $"at {Schedule.Hour:00}:{Schedule.Minute:00}";
    }


    protected static string GetDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Monday",
            DayOfWeek.Tuesday => "Tuesday",
            DayOfWeek.Wednesday => "Wednesday",
            DayOfWeek.Thursday => "Thursday",
            DayOfWeek.Friday => "Friday",
            DayOfWeek.Saturday => "Saturday",
            DayOfWeek.Sunday => "Sunday",
            _ => day.ToString()
        };
    }
}