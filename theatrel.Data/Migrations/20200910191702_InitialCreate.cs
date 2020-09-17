using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace theatrel.DataAccess.Migrations
{
    public partial class InitialCreate : Migration
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
                name: "PerformanceLocations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceTypes", x => x.Id);
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
                name: "Performances",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    TypeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Performances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Performances_PerformanceLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "PerformanceLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Performances_PerformanceTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "PerformanceTypes",
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

            migrationBuilder.CreateTable(
                name: "Playbill",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    When = table.Column<DateTime>(nullable: false),
                    Url = table.Column<string>(nullable: true),
                    PerformanceId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playbill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Playbill_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    PlaybillEntityId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceChanges_Playbill_PlaybillEntityId",
                        column: x => x.PlaybillEntityId,
                        principalTable: "Playbill",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceChanges_PlaybillEntityId",
                table: "PerformanceChanges",
                column: "PlaybillEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_LocationId",
                table: "Performances",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_TypeId",
                table: "Performances",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Playbill_PerformanceId",
                table: "Playbill",
                column: "PerformanceId");

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
                name: "Playbill");

            migrationBuilder.DropTable(
                name: "Filters");

            migrationBuilder.DropTable(
                name: "TlUsers");

            migrationBuilder.DropTable(
                name: "Performances");

            migrationBuilder.DropTable(
                name: "PerformanceLocations");

            migrationBuilder.DropTable(
                name: "PerformanceTypes");
        }
    }
}
