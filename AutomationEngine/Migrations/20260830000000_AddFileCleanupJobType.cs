using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationEngine.Migrations
{
    public partial class AddFileCleanupJobType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetFolder",
                table: "Jobs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileAgeInDays",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Recurse",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FileFilter",
                table: "Jobs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetFolder",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FileAgeInDays",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Recurse",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FileFilter",
                table: "Jobs");
        }
    }
}
