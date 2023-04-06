using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class VkSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VkSubscription",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubscriptionType = table.Column<int>(type: "integer", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TrackingChanges = table.Column<int>(type: "integer", nullable: false),
                    PerformanceFilterId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VkSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VkSubscription_Filters_PerformanceFilterId",
                        column: x => x.PerformanceFilterId,
                        principalTable: "Filters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VkSubscription_TlUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TlUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VkSubscription_PerformanceFilterId",
                table: "VkSubscription",
                column: "PerformanceFilterId");

            migrationBuilder.CreateIndex(
                name: "IX_VkSubscription_TelegramUserId",
                table: "VkSubscription",
                column: "TelegramUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VkSubscription");
        }
    }
}
