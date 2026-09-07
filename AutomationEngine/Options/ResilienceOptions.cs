using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Options
{
    /// <summary>
    /// Resilience policy configuration options.
    /// Binds to the "Resilience" section in appsettings.json.
    /// </summary>
    public class ResilienceOptions
    {
        public const string SectionName = "Resilience";

        [Range(0, 20)]
        public int RetryAttempts { get; set; } = 3;

        [Range(1, 300)]
        public int BackoffBaseSeconds { get; set; } = 2;

        [Range(1, 100)]
        public int CircuitBreakerThreshold { get; set; } = 5;

        [Range(1, 3600)]
        public int CircuitBreakerDurationSeconds { get; set; } = 30;
    }
}
