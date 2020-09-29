using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class fixedCharacterName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "СharacterName",
                table: "Roles");

            migrationBuilder.AddColumn<string>(
                name: "CharacterName",
                table: "Roles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CharacterName",
                table: "Roles");

            migrationBuilder.AddColumn<string>(
                name: "СharacterName",
                table: "Roles",
                type: "text",
                nullable: true);
        }
    }
}
