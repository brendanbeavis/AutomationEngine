using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationEngine.Migrations
{
    public partial class AddJobTypeAndScript : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "Jobs",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Script",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Jobs");
        }
    }
}
