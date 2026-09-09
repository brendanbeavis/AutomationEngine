using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Options
{
    /// <summary>
    /// Scheduler timing configuration options.
    /// Binds to the "Scheduler" section in appsettings.json.
    /// </summary>
    public class SchedulerOptions
    {
        public const string SectionName = "Scheduler";

        [Range(1, 3600)]
        public int ReloadIntervalSeconds { get; set; } = 60;

        [Range(1, 300)]
        public int LoopDelaySeconds { get; set; } = 5;
    }
}
