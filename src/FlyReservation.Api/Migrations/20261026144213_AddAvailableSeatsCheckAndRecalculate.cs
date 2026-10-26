using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyReservation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailableSeatsCheckAndRecalculate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
              UPDATE ""Flights"" f
              SET ""AvailableSeats"" = f.""TotalSeats"" - (
              SELECT COUNT(*) FROM ""Reservations"" r WHERE r.""FlightId"" = f.""Id""
             );");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Flights_AvailableSeats",
                table: "Flights",
                sql: "\"AvailableSeats\" >= 0 AND \"AvailableSeats\" <= \"TotalSeats\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Flights_AvailableSeats",
                table: "Flights");
        }
    }
}
