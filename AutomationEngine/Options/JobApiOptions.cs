using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Options
{
    /// <summary>
    /// Job API client behavior options.
    /// Binds to the "JobApi" section in appsettings.json.
    /// </summary>
    public class JobApiOptions
    {
        public const string SectionName = "JobApi";

        [Range(1, 5000)]
        public int HistoryLimit { get; set; } = 50;
    }
}
