using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Options
{
    /// <summary>
    /// Design-time database configuration options for EF tooling.
    /// Binds to the "DesignTimeDatabase" section in appsettings.json.
    /// </summary>
    public class DesignTimeDatabaseOptions
    {
        public const string SectionName = "DesignTimeDatabase";

        [Required]
        [StringLength(256)]
        public string Provider { get; set; } = "Sqlite";

        [Required]
        [StringLength(1024)]
        public string FilePath { get; set; } = "db\\AutomationEngine.db";

        [Range(0, 600)]
        public int CommandTimeoutSeconds { get; set; } = 60;

        [Range(1, 1000)]
        public int PoolSize { get; set; } = 5;

        public bool SqliteEnableWal { get; set; } = false;

        public bool SqliteCacheQueryPlans { get; set; } = true;
    }
}
