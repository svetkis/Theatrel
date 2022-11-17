using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    public partial class ShortDescrLocations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "PerformanceLocations",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "PerformanceLocations");
        }
    }
}
