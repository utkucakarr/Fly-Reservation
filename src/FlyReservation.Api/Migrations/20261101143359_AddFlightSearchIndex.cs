using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyReservation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Flights_Origin_Destination_DepartureTime",
                table: "Flights",
                columns: new[] { "Origin", "Destination", "DepartureTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Flights_Origin_Destination_DepartureTime",
                table: "Flights");
        }
    }
}
