using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ŠišAppApi.Migrations
{
    /// <inheritdoc />
    public partial class StoreImageBytesInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "Images",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileData",
                table: "Images");
        }
    }
}
