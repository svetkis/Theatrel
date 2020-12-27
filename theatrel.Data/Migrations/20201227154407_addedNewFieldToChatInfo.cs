using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class addedNewFieldToChatInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Info",
                table: "TlChats",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Info",
                table: "TlChats");
        }
    }
}
