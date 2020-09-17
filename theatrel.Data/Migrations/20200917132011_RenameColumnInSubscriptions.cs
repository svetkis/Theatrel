using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class RenameColumnInSubscriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerformanceId",
                table: "Filters");

            migrationBuilder.AddColumn<int>(
                name: "PlaybillId",
                table: "Filters",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaybillId",
                table: "Filters");

            migrationBuilder.AddColumn<int>(
                name: "PerformanceId",
                table: "Filters",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
