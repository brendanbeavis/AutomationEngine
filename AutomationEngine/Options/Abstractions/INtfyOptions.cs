namespace AutomationEngine.Options.Interfaces
{
    /// <summary>
    /// Typed options for Ntfy push notification configuration
    /// Binds to the "Ntfy" section in appsettings.json
    /// </summary>
    public interface INtfyOptions
    {
        /// <summary>
        /// Whether Ntfy notifications are enabled
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Ntfy service endpoint URL
        /// Example: https://ntfy.sh/your-topic
        /// </summary>
        string? Endpoint { get; }
    }
}
