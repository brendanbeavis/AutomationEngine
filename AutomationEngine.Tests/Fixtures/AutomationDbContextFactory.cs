using AutomationEngine.Data;
using Microsoft.EntityFrameworkCore;

namespace AutomationEngine.Tests.Fixtures
{
    /// <summary>
    /// Factory for creating test instances of AutomationDbContext
    /// Uses in-memory SQLite for isolation and speed
    /// </summary>
    public class AutomationDbContextFactory
    {
        private static int _dbCounter = 0;

        /// <summary>
        /// Creates a new isolated in-memory database context for each test
        /// </summary>
        public static AutomationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseSqlite($"Data Source=:memory:;")
                .Options;

            var context = new AutomationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Creates a new isolated SQLite file-based database for tests that need persistence
        /// </summary>
        public static AutomationDbContext CreateFilePersistenceContext()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseSqlite($"Data Source={dbPath};")
                .Options;

            var context = new AutomationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Clean up a test database file if needed
        /// </summary>
        public static void CleanupContext(AutomationDbContext context, string? dbPath = null)
        {
            context?.Dispose();

            if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
            {
                try { File.Delete(dbPath); }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }
}
