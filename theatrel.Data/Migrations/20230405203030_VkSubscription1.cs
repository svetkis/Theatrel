using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class VkSubscription1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VkSubscription_TlUsers_TelegramUserId",
                table: "VkSubscription");

            migrationBuilder.DropIndex(
                name: "IX_VkSubscription_TelegramUserId",
                table: "VkSubscription");

            migrationBuilder.RenameColumn(
                name: "TelegramUserId",
                table: "VkSubscription",
                newName: "VkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VkId",
                table: "VkSubscription",
                newName: "TelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VkSubscription_TelegramUserId",
                table: "VkSubscription",
                column: "TelegramUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_VkSubscription_TlUsers_TelegramUserId",
                table: "VkSubscription",
                column: "TelegramUserId",
                principalTable: "TlUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
