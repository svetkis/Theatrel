using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PerfofmanceReadyToDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReadyToDelete",
                table: "Playbill",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReadyToDelete",
                table: "Playbill");
        }
    }
}
