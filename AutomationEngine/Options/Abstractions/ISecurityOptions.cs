namespace AutomationEngine.Options.Interfaces
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
    }
}
