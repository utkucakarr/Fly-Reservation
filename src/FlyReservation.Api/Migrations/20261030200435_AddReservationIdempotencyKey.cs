using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyReservation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Reservations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdempotencyKey",
                table: "Reservations",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_IdempotencyKey",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Reservations");
        }
    }
}
