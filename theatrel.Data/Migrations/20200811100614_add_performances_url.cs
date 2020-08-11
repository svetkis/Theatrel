using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class add_performances_url : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceDataId",
                table: "PerformanceChanges");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceChanges_PerformanceDataId",
                table: "PerformanceChanges");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Performances",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerformanceEntityId",
                table: "PerformanceChanges",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceChanges_PerformanceEntityId",
                table: "PerformanceChanges",
                column: "PerformanceEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceEntityId",
                table: "PerformanceChanges",
                column: "PerformanceEntityId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceEntityId",
                table: "PerformanceChanges");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceChanges_PerformanceEntityId",
                table: "PerformanceChanges");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "PerformanceEntityId",
                table: "PerformanceChanges");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceChanges_PerformanceDataId",
                table: "PerformanceChanges",
                column: "PerformanceDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceDataId",
                table: "PerformanceChanges",
                column: "PerformanceDataId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
