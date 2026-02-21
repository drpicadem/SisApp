using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ŠišAppApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBarberResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BarberRespondedAt",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BarberResponse",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BarberRespondedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "BarberResponse",
                table: "Reviews");
        }
    }
}
