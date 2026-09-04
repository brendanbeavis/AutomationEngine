using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "cyberpunk"),
                    LoggingLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Information"),
                    JobExecutionEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    DefaultJobTimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 300),
                    EnableDetailedLogging = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    MaxJobHistoryRecords = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1000),
                    ApplicationName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false, defaultValue: "Automation Engine"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            // Insert default settings
            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "Theme", "LoggingLevel", "JobExecutionEnabled", "DefaultJobTimeoutSeconds", "EnableDetailedLogging", "MaxJobHistoryRecords", "ApplicationName", "UpdatedAt", "CreatedAt" },
                values: new object[] { 1, "cyberpunk", "Information", true, 300, false, 1000, "Automation Engine", DateTime.UtcNow, DateTime.UtcNow });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
