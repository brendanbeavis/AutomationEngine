using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Options
{
    /// <summary>
    /// Ntfy push notification configuration options
    /// Binds to the "Ntfy" section in appsettings.json
    /// </summary>
    public class NtfyOptions : INtfyOptions
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string SectionName = "Ntfy";

        /// <summary>
        /// Whether Ntfy notifications are enabled
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Ntfy service endpoint URL
        /// Example: https://ntfy.sh/your-topic
        /// </summary>
        [Url]
        [StringLength(1000)]
        public string? Endpoint { get; set; }
    }
}
