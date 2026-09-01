namespace AutomationEngine.Options
{
    /// <summary>
    /// Server configuration options
    /// Binds to the "Server" section in appsettings.json
    /// Provides configurable port and computed URL properties
    /// </summary>
    public class ServerOptions : IServerOptions
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string SectionName = "Server";

        /// <summary>
        /// The port number the application listens on
        /// Default: 5000
        /// Can be overridden via appsettings.json Server:Port setting
        /// or via environment variable Server__Port
        /// </summary>
        public int Port { get; set; } = 5000;

        /// <summary>
        /// The localhost URL for the application (http://localhost:PORT)
        /// Computed from the Port property
        /// Used in CORS configuration and startup logging
        /// </summary>
        public string LocalhostUrl => $"http://localhost:{Port}";

        /// <summary>
        /// The loopback URL for the application (http://127.0.0.1:PORT)
        /// Computed from the Port property
        /// Used in CORS configuration
        /// </summary>
        public string LoopbackUrl => $"http://127.0.0.1:{Port}";
    }
}
