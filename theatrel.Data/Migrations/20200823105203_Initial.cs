using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace theatrel.DataAccess.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Performances",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Location = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false),
                    Url = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Performances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TlChats",
                columns: table => new
                {
                    ChatId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Culture = table.Column<string>(nullable: true),
                    CurrentStepId = table.Column<int>(nullable: false),
                    PreviousStepId = table.Column<int>(nullable: false),
                    When = table.Column<DateTime>(nullable: false),
                    DbDays = table.Column<string>(nullable: true),
                    DbTypes = table.Column<string>(nullable: true),
                    LastMessage = table.Column<DateTime>(nullable: false),
                    DialogState = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TlChats", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "TlUsers",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Culture = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TlUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceChanges",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinPrice = table.Column<int>(nullable: false),
                    ReasonOfChanges = table.Column<int>(nullable: false),
                    LastUpdate = table.Column<DateTime>(nullable: false),
                    PerformanceEntityId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceChanges_Performances_PerformanceEntityId",
                        column: x => x.PerformanceEntityId,
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(nullable: false),
                    LastUpdate = table.Column<DateTime>(nullable: false),
                    TrackingChanges = table.Column<int>(nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Subscriptions_TlUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TlUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceChanges_PerformanceEntityId",
                table: "PerformanceChanges",
                column: "PerformanceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PerformanceFilterId",
                table: "Subscriptions",
                column: "PerformanceFilterId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TelegramUserId",
                table: "Subscriptions",
                column: "TelegramUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceChanges");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "TlChats");

            migrationBuilder.DropTable(
                name: "Performances");

            migrationBuilder.DropTable(
                name: "Filters");

            migrationBuilder.DropTable(
                name: "TlUsers");
        }
    }
}
