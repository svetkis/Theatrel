using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    public partial class AddActorToFilterAndChatInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Actor",
                table: "TlChats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Actor",
                table: "Filters",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Actor",
                table: "TlChats");

            migrationBuilder.DropColumn(
                name: "Actor",
                table: "Filters");
        }
    }
}
