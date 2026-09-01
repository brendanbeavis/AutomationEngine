using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndAuditLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentState",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileAgeInDays",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileFilter",
                table: "Jobs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Recurse",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RunningProcessId",
                table: "Jobs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RunningStartTime",
                table: "Jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetFolder",
                table: "Jobs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "JobRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LoggingLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    JobExecutionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultJobTimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableEmailNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    EnableDetailedLogging = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxJobHistoryRecords = table.Column<int>(type: "INTEGER", nullable: false),
                    ApplicationName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: true),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_JobId",
                table: "AuditLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_JobId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "JobId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CurrentState",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FileAgeInDays",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FileFilter",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Recurse",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RunningProcessId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RunningStartTime",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TargetFolder",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "JobRuns");
        }
    }
}
