using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyReservation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Passengers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Passengers");
        }
    }
}
