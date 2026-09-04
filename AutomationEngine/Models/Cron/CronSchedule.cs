namespace AutomationEngine.Models.Cron;

/// <summary>
/// Represents a cron schedule configuration (data model)
/// Use CronScheduleParser to create instances from cron expressions
/// </summary>
public sealed class CronSchedule
{
    public CronScheduleType ScheduleType { get; set; } = CronScheduleType.EveryMinute;

    public int MinuteInterval { get; set; } = 5;

    public int HourInterval { get; set; } = 1;

    public int Hour { get; set; } = 0;

    public int Minute { get; set; } = 0;

    public HashSet<DayOfWeek> DaysOfWeek { get; set; } = [];

    public int DayOfMonth { get; set; } = 1;

    public string CustomExpression { get; set; } = "* * * * *";
}
