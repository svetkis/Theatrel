using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    public partial class ChangesAddRemoveCast : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CastAdded",
                table: "PlaybillChanges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CastRemoved",
                table: "PlaybillChanges",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CastAdded",
                table: "PlaybillChanges");

            migrationBuilder.DropColumn(
                name: "CastRemoved",
                table: "PlaybillChanges");
        }
    }
}
