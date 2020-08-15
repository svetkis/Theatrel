using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace theatrel.DataAccess.Migrations
{
    public partial class SubscriptionsFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceChangeEntity_Performances_PerformanceEntityId",
                table: "PerformanceChangeEntity");

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

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(nullable: false),
                    LastUpdate = table.Column<DateTime>(nullable: false),
                    PerformanceFilterId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Filters_PerformanceFilterId",
                        column: x => x.PerformanceFilterId,
                        principalTable: "Filters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PerformanceFilterId",
                table: "Subscriptions",
                column: "PerformanceFilterId");

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

            migrationBuilder.DropTable(
                name: "Subscriptions");

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

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceChangeEntity_Performances_PerformanceEntityId",
                table: "PerformanceChangeEntity",
                column: "PerformanceEntityId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
