using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    public partial class ChangesRemoveAutoProlongation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoProlongation",
                table: "Subscriptions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoProlongation",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
