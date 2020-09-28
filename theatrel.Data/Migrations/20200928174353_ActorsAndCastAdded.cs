using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace theatrel.DataAccess.Migrations
{
    public partial class ActorsAndCastAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActorEntity",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActorEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleEntity",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    СharacterName = table.Column<string>(nullable: true),
                    PerformanceId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleEntity_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActorInRoleEntity",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorId = table.Column<int>(nullable: false),
                    RoleId = table.Column<int>(nullable: false),
                    PlaybillEntityId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActorInRoleEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActorInRoleEntity_ActorEntity_ActorId",
                        column: x => x.ActorId,
                        principalTable: "ActorEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActorInRoleEntity_Playbill_PlaybillEntityId",
                        column: x => x.PlaybillEntityId,
                        principalTable: "Playbill",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActorInRoleEntity_RoleEntity_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActorInRoleEntity_ActorId",
                table: "ActorInRoleEntity",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ActorInRoleEntity_PlaybillEntityId",
                table: "ActorInRoleEntity",
                column: "PlaybillEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActorInRoleEntity_RoleId",
                table: "ActorInRoleEntity",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleEntity_PerformanceId",
                table: "RoleEntity",
                column: "PerformanceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActorInRoleEntity");

            migrationBuilder.DropTable(
                name: "ActorEntity");

            migrationBuilder.DropTable(
                name: "RoleEntity");
        }
    }
}
