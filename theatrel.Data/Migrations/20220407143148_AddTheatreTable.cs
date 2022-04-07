using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    public partial class AddTheatreTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "When",
                table: "TlChats",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMessage",
                table: "TlChats",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<string>(
                name: "DbTheatres",
                table: "TlChats",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdate",
                table: "PlaybillChanges",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "When",
                table: "Playbill",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<long>(
                name: "TheatreId",
                table: "PerformanceLocations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Filters",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Filters",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<string>(
                name: "DbTheatres",
                table: "Filters",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Theatre",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Theatre", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceLocations_TheatreId",
                table: "PerformanceLocations",
                column: "TheatreId");

            migrationBuilder.AddForeignKey(
                name: "FK_PerformanceLocations_Theatre_TheatreId",
                table: "PerformanceLocations",
                column: "TheatreId",
                principalTable: "Theatre",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PerformanceLocations_Theatre_TheatreId",
                table: "PerformanceLocations");

            migrationBuilder.DropTable(
                name: "Theatre");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceLocations_TheatreId",
                table: "PerformanceLocations");

            migrationBuilder.DropColumn(
                name: "DbTheatres",
                table: "TlChats");

            migrationBuilder.DropColumn(
                name: "TheatreId",
                table: "PerformanceLocations");

            migrationBuilder.DropColumn(
                name: "DbTheatres",
                table: "Filters");

            migrationBuilder.AlterColumn<DateTime>(
                name: "When",
                table: "TlChats",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMessage",
                table: "TlChats",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdate",
                table: "Subscriptions",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdate",
                table: "PlaybillChanges",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "When",
                table: "Playbill",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Filters",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Filters",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
