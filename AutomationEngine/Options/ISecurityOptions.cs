namespace AutomationEngine.Options
{
    /// <summary>
    /// Typed options for security configuration
    /// Binds to the "Security" section in appsettings.json
    /// </summary>
    public interface ISecurityOptions
    {
        /// <summary>
        /// Whether to restrict access to localhost only
        /// </summary>
        bool LocalhostOnly { get; }

        /// <summary>
        /// Admin secret for programmatic API access
        /// Null/empty means admin secret validation is disabled
        /// </summary>
        string? AdminSecret { get; }

        /// <summary>
        /// Description of the admin secret setting
        /// </summary>
        string? AdminSecretDescription { get; }
    }
}
