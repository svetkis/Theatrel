using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class AddNameToFilters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PerformanceName",
                table: "TlChats",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformanceName",
                table: "Filters",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerformanceName",
                table: "TlChats");

            migrationBuilder.DropColumn(
                name: "PerformanceName",
                table: "Filters");
        }
    }
}
