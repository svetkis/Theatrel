using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class PerformanceChanes_Fixed_PerformanceId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceEntityId",
                table: "PerformanceChanges");

            migrationBuilder.DropColumn(
                name: "PerformanceDataId",
                table: "PerformanceChanges");

            migrationBuilder.AlterColumn<int>(
                name: "PerformanceEntityId",
                table: "PerformanceChanges",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceEntityId",
                table: "PerformanceChanges",
                column: "PerformanceEntityId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceEntityId",
                table: "PerformanceChanges");

            migrationBuilder.AlterColumn<int>(
                name: "PerformanceEntityId",
                table: "PerformanceChanges",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "PerformanceDataId",
                table: "PerformanceChanges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceEntityId",
                table: "PerformanceChanges",
                column: "PerformanceEntityId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
