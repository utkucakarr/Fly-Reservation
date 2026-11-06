using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyReservation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_FlightId_PassengerId",
                table: "Reservations");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDueAt",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTransactionId",
                table: "Reservations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Reservations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Confirmed");

            // The default was only for the rows above. From now on the code must always set a status
            migrationBuilder.Sql("ALTER TABLE \"Reservations\" ALTER COLUMN \"Status\" DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_FlightId_PassengerId",
                table: "Reservations",
                columns: new[] { "FlightId", "PassengerId" },
                unique: true,
                filter: "\"Status\" <> 'Expired'");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PaymentDueAt",
                table: "Reservations",
                column: "PaymentDueAt",
                filter: "\"Status\" = 'PendingPayment'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_FlightId_PassengerId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PaymentDueAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentDueAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_FlightId_PassengerId",
                table: "Reservations",
                columns: new[] { "FlightId", "PassengerId" },
                unique: true);
        }
    }
}
