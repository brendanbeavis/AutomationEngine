using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationEngine.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Command = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Arguments = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WorkingDirectory = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Schedule = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Retry = table.Column<int>(type: "INTEGER", nullable: false),
                    OnFailureNotify = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: false),
                    StdOut = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    StdErr = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobRuns_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobRuns_JobId",
                table: "JobRuns",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobId",
                table: "Jobs",
                column: "JobId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobRuns");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
