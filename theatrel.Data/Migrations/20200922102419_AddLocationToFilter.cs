using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class AddLocationToFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DbLocations",
                table: "TlChats",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DbLocations",
                table: "TlChats");
        }
    }
}
