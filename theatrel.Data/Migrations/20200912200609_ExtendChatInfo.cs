using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace theatrel.DataAccess.Migrations
{
    public partial class ExtendChatInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TlChats",
                table: "TlChats");

            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "TlChats");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "TlChats",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "CommandLine",
                table: "TlChats",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TlChats",
                table: "TlChats",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TlChats",
                table: "TlChats");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TlChats");

            migrationBuilder.DropColumn(
                name: "CommandLine",
                table: "TlChats");

            migrationBuilder.AddColumn<long>(
                name: "ChatId",
                table: "TlChats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TlChats",
                table: "TlChats",
                column: "ChatId");
        }
    }
}
