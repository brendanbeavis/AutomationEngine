using AutomationEngine.Options.Interfaces;
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

    }
}
