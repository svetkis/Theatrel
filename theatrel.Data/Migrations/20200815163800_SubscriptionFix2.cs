using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class SubscriptionFix2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PerformanceDateTime",
                table: "Performances");

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                table: "TlUsers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                table: "Subscriptions",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TelegramUserId",
                table: "Subscriptions",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "TrackingChanges",
                table: "Subscriptions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTime",
                table: "Performances",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Performances",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinPrice",
                table: "Performances",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Performances",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Performances",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TelegramUserId",
                table: "Subscriptions",
                column: "TelegramUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_TlUsers_TelegramUserId",
                table: "Subscriptions",
                column: "TelegramUserId",
                principalTable: "TlUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_TlUsers_TelegramUserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TelegramUserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Culture",
                table: "TlUsers");

            migrationBuilder.DropColumn(
                name: "Culture",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TrackingChanges",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "DateTime",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "MinPrice",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Performances");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Subscriptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "PerformanceDateTime",
                table: "Performances",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
