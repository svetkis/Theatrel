using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    public partial class RemoveFromFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartOfDay",
                table: "Filters");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartOfDay",
                table: "Filters",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
