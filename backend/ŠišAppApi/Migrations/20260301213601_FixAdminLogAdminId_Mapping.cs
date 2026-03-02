using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ŠišAppApi.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminLogAdminId_Mapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminLogs_Admins_AdminId1",
                table: "AdminLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AdminLogs_Users_AdminId",
                table: "AdminLogs");

            migrationBuilder.DropIndex(
                name: "IX_AdminLogs_AdminId1",
                table: "AdminLogs");

            migrationBuilder.DropColumn(
                name: "AdminId1",
                table: "AdminLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLogs_Admins_AdminId",
                table: "AdminLogs",
                column: "AdminId",
                principalTable: "Admins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminLogs_Admins_AdminId",
                table: "AdminLogs");

            migrationBuilder.AddColumn<int>(
                name: "AdminId1",
                table: "AdminLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminLogs_AdminId1",
                table: "AdminLogs",
                column: "AdminId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLogs_Admins_AdminId1",
                table: "AdminLogs",
                column: "AdminId1",
                principalTable: "Admins",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLogs_Users_AdminId",
                table: "AdminLogs",
                column: "AdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
