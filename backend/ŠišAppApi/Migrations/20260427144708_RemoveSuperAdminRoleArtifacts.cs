using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ŠišAppApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSuperAdminRoleArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Users] SET [Role] = 'Admin' WHERE [Role] = 'SuperAdmin';");
            migrationBuilder.Sql("UPDATE [Admins] SET [Role] = 'Admin' WHERE [Role] = 'SuperAdmin';");

            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                table: "Admins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "Admins",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
