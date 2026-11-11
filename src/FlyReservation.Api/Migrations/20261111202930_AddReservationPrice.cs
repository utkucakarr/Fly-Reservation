using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyReservation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Reservations",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
            // Old reservations: the price at booking was never stored. The best we have is the flight's current price
            migrationBuilder.Sql("""
                UPDATE "Reservations" AS r
                SET "Price" = f."Price"
                FROM "Flights" AS f
                WHERE f."Id" = r."FlightId";
                """);

            // The default was only for the rows above. From now on the code must always set a price
            migrationBuilder.Sql("""ALTER TABLE "Reservations" ALTER COLUMN "Price" DROP DEFAULT;""");

            // ↑↑↑ new ↑↑↑

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_Price",
                table: "Reservations",
                sql: "\"Price\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_Price",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Reservations");
        }
    }
}
