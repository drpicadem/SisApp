using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ŠišAppApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCitiesLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Salons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Salons_CityId",
                table: "Salons",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Name",
                table: "Cities",
                column: "Name",
                unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO Cities (Name, Country)
                SELECT DISTINCT LTRIM(RTRIM(s.City)), 'Bosna i Hercegovina'
                FROM Salons s
                WHERE s.City IS NOT NULL
                  AND LTRIM(RTRIM(s.City)) <> ''
                  AND NOT EXISTS (
                      SELECT 1
                      FROM Cities c
                      WHERE c.Name = LTRIM(RTRIM(s.City))
                  )
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Cities WHERE Name = 'Zenica')
                BEGIN
                    INSERT INTO Cities (Name, Country) VALUES ('Zenica', 'Bosna i Hercegovina')
                END
            ");

            migrationBuilder.Sql(@"
                UPDATE s
                SET s.CityId = c.Id
                FROM Salons s
                LEFT JOIN Cities c ON c.Name = s.City
            ");

            migrationBuilder.Sql(@"
                UPDATE Salons
                SET CityId = (SELECT TOP 1 Id FROM Cities WHERE Name = 'Zenica')
                WHERE CityId IS NULL
            ");

            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Salons",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Salons_Cities_CityId",
                table: "Salons",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "City",
                table: "Salons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Salons_Cities_CityId",
                table: "Salons");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_Salons_CityId",
                table: "Salons");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Salons");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Salons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
