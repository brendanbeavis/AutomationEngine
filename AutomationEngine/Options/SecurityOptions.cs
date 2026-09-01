using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Options
{
    /// <summary>
    /// Security configuration options
    /// Binds to the "Security" section in appsettings.json
    /// </summary>
    public class SecurityOptions : ISecurityOptions
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string SectionName = "Security";

        /// <summary>
        /// Whether to restrict access to localhost only
        /// </summary>
        public bool LocalhostOnly { get; set; } = true;

        /// <summary>
        /// Admin secret for programmatic API access
        /// Null/empty means admin secret validation is disabled
        /// </summary>
        [StringLength(500)]
        public string? AdminSecret { get; set; }

        /// <summary>
        /// Description of the admin secret setting
        /// </summary>
        [StringLength(1000)]
        public string? AdminSecretDescription { get; set; }
    }
}
