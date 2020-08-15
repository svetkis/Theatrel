using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace theatrel.DataAccess.Migrations
{
    public partial class Subscriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceChanges_Performances_PerformanceEntityId",
                table: "PerformanceChanges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PerformanceChanges",
                table: "PerformanceChanges");

            migrationBuilder.RenameTable(
                name: "PerformanceChanges",
                newName: "PerformanceChangeEntity");

            migrationBuilder.RenameIndex(
                name: "IX_PerformanceChanges_PerformanceEntityId",
                table: "PerformanceChangeEntity",
                newName: "IX_PerformanceChangeEntity_PerformanceEntityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PerformanceChangeEntity",
                table: "PerformanceChangeEntity",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Filters",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DbDaysOfWeek = table.Column<string>(nullable: true),
                    DbPerformanceTypes = table.Column<string>(nullable: true),
                    DbLocations = table.Column<string>(nullable: true),
                    PartOfDay = table.Column<int>(nullable: false),
                    StartDate = table.Column<DateTime>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: false),
                    PerformanceId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filters", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceChangeEntity_Performances_PerformanceEntityId",
                table: "PerformanceChangeEntity",
                column: "PerformanceEntityId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceChangeEntity_Performances_PerformanceEntityId",
                table: "PerformanceChangeEntity");

            migrationBuilder.DropTable(
                name: "Filters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PerformanceChangeEntity",
                table: "PerformanceChangeEntity");

            migrationBuilder.RenameTable(
                name: "PerformanceChangeEntity",
                newName: "PerformanceChanges");

            migrationBuilder.RenameIndex(
                name: "IX_PerformanceChangeEntity_PerformanceEntityId",
                table: "PerformanceChanges",
                newName: "IX_PerformanceChanges_PerformanceEntityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PerformanceChanges",
                table: "PerformanceChanges",
                column: "Id");

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
