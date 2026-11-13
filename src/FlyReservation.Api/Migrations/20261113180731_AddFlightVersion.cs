using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyReservation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-written: EF scaffolded "rename xmin to Version", but xmin is a PostgreSQL system column
            // (0A000: cannot rename system column "xmin"). xmin stays as it is; Version is a new column.
            // Existing flights start at version 0
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Flights",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Flights");
        }
    }
}
