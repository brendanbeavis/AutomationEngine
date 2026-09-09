namespace AutomationEngine.Options.Interfaces
{
    /// <summary>
    /// Typed options for server configuration
    /// Binds to the "Server" section in appsettings.json
    /// </summary>
    public interface IServerOptions
    {
        /// <summary>
        /// The port number the application listens on
        /// Default: 5000
        /// Can be overridden via appsettings.json or environment variables
        /// </summary>
        int Port { get; }

        /// <summary>
        /// The localhost URL for the application
        /// Combines http://localhost with the configured port
        /// Used in CORS configuration and startup logging
        /// </summary>
        string LocalhostUrl { get; }

        /// <summary>
        /// The loopback URL for the application (127.0.0.1)
        /// Combines http://127.0.0.1 with the configured port
        /// Used in CORS configuration
        /// </summary>
        string LoopbackUrl { get; }


    }
}
