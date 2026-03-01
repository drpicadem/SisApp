using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ŠišAppApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteSalons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteSalons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SalonId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteSalons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteSalons_Salons_SalonId",
                        column: x => x.SalonId,
                        principalTable: "Salons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteSalons_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteSalons_SalonId",
                table: "FavoriteSalons",
                column: "SalonId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteSalons_UserId",
                table: "FavoriteSalons",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteSalons");
        }
    }
}
