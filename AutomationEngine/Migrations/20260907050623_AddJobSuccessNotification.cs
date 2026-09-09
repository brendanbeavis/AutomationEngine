using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSuccessNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OnSuccessNotify",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnSuccessNotify",
                table: "Jobs");
        }
    }
}
