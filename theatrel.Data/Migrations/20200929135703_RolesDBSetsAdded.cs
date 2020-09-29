using Microsoft.EntityFrameworkCore.Migrations;

namespace theatrel.DataAccess.Migrations
{
    public partial class RolesDBSetsAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActorInRoleEntity_ActorEntity_ActorId",
                table: "ActorInRoleEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_ActorInRoleEntity_Playbill_PlaybillEntityId",
                table: "ActorInRoleEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_ActorInRoleEntity_RoleEntity_RoleId",
                table: "ActorInRoleEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleEntity_Performances_PerformanceId",
                table: "RoleEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleEntity",
                table: "RoleEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActorInRoleEntity",
                table: "ActorInRoleEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActorEntity",
                table: "ActorEntity");

            migrationBuilder.RenameTable(
                name: "RoleEntity",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "ActorInRoleEntity",
                newName: "ActorInRole");

            migrationBuilder.RenameTable(
                name: "ActorEntity",
                newName: "Actors");

            migrationBuilder.RenameIndex(
                name: "IX_RoleEntity_PerformanceId",
                table: "Roles",
                newName: "IX_Roles_PerformanceId");

            migrationBuilder.RenameIndex(
                name: "IX_ActorInRoleEntity_RoleId",
                table: "ActorInRole",
                newName: "IX_ActorInRole_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_ActorInRoleEntity_PlaybillEntityId",
                table: "ActorInRole",
                newName: "IX_ActorInRole_PlaybillEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_ActorInRoleEntity_ActorId",
                table: "ActorInRole",
                newName: "IX_ActorInRole_ActorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActorInRole",
                table: "ActorInRole",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Actors",
                table: "Actors",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActorInRole_Actors_ActorId",
                table: "ActorInRole",
                column: "ActorId",
                principalTable: "Actors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActorInRole_Playbill_PlaybillEntityId",
                table: "ActorInRole",
                column: "PlaybillEntityId",
                principalTable: "Playbill",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActorInRole_Roles_RoleId",
                table: "ActorInRole",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Performances_PerformanceId",
                table: "Roles",
                column: "PerformanceId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActorInRole_Actors_ActorId",
                table: "ActorInRole");

            migrationBuilder.DropForeignKey(
                name: "FK_ActorInRole_Playbill_PlaybillEntityId",
                table: "ActorInRole");

            migrationBuilder.DropForeignKey(
                name: "FK_ActorInRole_Roles_RoleId",
                table: "ActorInRole");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Performances_PerformanceId",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Actors",
                table: "Actors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActorInRole",
                table: "ActorInRole");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "RoleEntity");

            migrationBuilder.RenameTable(
                name: "Actors",
                newName: "ActorEntity");

            migrationBuilder.RenameTable(
                name: "ActorInRole",
                newName: "ActorInRoleEntity");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_PerformanceId",
                table: "RoleEntity",
                newName: "IX_RoleEntity_PerformanceId");

            migrationBuilder.RenameIndex(
                name: "IX_ActorInRole_RoleId",
                table: "ActorInRoleEntity",
                newName: "IX_ActorInRoleEntity_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_ActorInRole_PlaybillEntityId",
                table: "ActorInRoleEntity",
                newName: "IX_ActorInRoleEntity_PlaybillEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_ActorInRole_ActorId",
                table: "ActorInRoleEntity",
                newName: "IX_ActorInRoleEntity_ActorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleEntity",
                table: "RoleEntity",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActorEntity",
                table: "ActorEntity",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActorInRoleEntity",
                table: "ActorInRoleEntity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActorInRoleEntity_ActorEntity_ActorId",
                table: "ActorInRoleEntity",
                column: "ActorId",
                principalTable: "ActorEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActorInRoleEntity_Playbill_PlaybillEntityId",
                table: "ActorInRoleEntity",
                column: "PlaybillEntityId",
                principalTable: "Playbill",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActorInRoleEntity_RoleEntity_RoleId",
                table: "ActorInRoleEntity",
                column: "RoleId",
                principalTable: "RoleEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleEntity_Performances_PerformanceId",
                table: "RoleEntity",
                column: "PerformanceId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
