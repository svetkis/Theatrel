using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class RenameChangesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceChanges_Playbill_PlaybillEntityId",
                table: "PerformanceChanges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PerformanceChanges",
                table: "PerformanceChanges");

            migrationBuilder.RenameTable(
                name: "PerformanceChanges",
                newName: "PlaybillChanges");

            migrationBuilder.RenameIndex(
                name: "IX_PerformanceChanges_PlaybillEntityId",
                table: "PlaybillChanges",
                newName: "IX_PlaybillChanges_PlaybillEntityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlaybillChanges",
                table: "PlaybillChanges",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaybillChanges_Playbill_PlaybillEntityId",
                table: "PlaybillChanges",
                column: "PlaybillEntityId",
                principalTable: "Playbill",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaybillChanges_Playbill_PlaybillEntityId",
                table: "PlaybillChanges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlaybillChanges",
                table: "PlaybillChanges");

            migrationBuilder.RenameTable(
                name: "PlaybillChanges",
                newName: "PerformanceChanges");

            migrationBuilder.RenameIndex(
                name: "IX_PlaybillChanges_PlaybillEntityId",
                table: "PerformanceChanges",
                newName: "IX_PerformanceChanges_PlaybillEntityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PerformanceChanges",
                table: "PerformanceChanges",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceChanges_Playbill_PlaybillEntityId",
                table: "PerformanceChanges",
                column: "PlaybillEntityId",
                principalTable: "Playbill",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
