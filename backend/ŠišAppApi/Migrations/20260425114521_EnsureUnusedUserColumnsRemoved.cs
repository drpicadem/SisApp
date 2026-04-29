using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ŠišAppApi.Migrations
{
    /// <inheritdoc />
    public partial class EnsureUnusedUserColumnsRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'EmailVerifiedAt') IS NOT NULL
                    ALTER TABLE [Users] DROP COLUMN [EmailVerifiedAt];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'PhoneVerifiedAt') IS NOT NULL
                    ALTER TABLE [Users] DROP COLUMN [PhoneVerifiedAt];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'LastLoginIp') IS NOT NULL
                    ALTER TABLE [Users] DROP COLUMN [LastLoginIp];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'Preferences') IS NOT NULL
                    ALTER TABLE [Users] DROP COLUMN [Preferences];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'EmailVerifiedAt') IS NULL
                    ALTER TABLE [Users] ADD [EmailVerifiedAt] datetime2 NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'PhoneVerifiedAt') IS NULL
                    ALTER TABLE [Users] ADD [PhoneVerifiedAt] datetime2 NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'LastLoginIp') IS NULL
                    ALTER TABLE [Users] ADD [LastLoginIp] nvarchar(50) NULL;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'Preferences') IS NULL
                    ALTER TABLE [Users] ADD [Preferences] nvarchar(max) NULL;
                """);
        }
    }
}
