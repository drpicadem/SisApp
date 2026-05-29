using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ŠišAppApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentAuditFieldsAndPaymentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByUserId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CancelledByUserId",
                table: "Appointments",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ConfirmedByUserId",
                table: "Appointments",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSessions_UserId",
                table: "PaymentSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_CancelledByUserId",
                table: "Appointments",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_ConfirmedByUserId",
                table: "Appointments",
                column: "ConfirmedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Users_CancelledByUserId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Users_ConfirmedByUserId",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "PaymentSessions");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CancelledByUserId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ConfirmedByUserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "Appointments");
        }
    }
}
