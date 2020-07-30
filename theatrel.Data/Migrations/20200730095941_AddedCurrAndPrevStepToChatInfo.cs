using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class AddedCurrAndPrevStepToChatInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatStep",
                table: "TlChats");

            migrationBuilder.AddColumn<int>(
                name: "CurrentStepId",
                table: "TlChats",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreviousStepId",
                table: "TlChats",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStepId",
                table: "TlChats");

            migrationBuilder.DropColumn(
                name: "PreviousStepId",
                table: "TlChats");

            migrationBuilder.AddColumn<int>(
                name: "ChatStep",
                table: "TlChats",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
