using FlyReservation.Api.Entities;
using FlyReservation.Api.Exceptions;

namespace FlyReservation.UnitTests.Entities
{
    public class FlightTests
    {
        private static Flight CreateFlight(int totalSeats, int availableSeats)
        {
            return new Flight
            {
                FlightNumber = "XQ1",
                TotalSeats = totalSeats,
                AvailableSeats = availableSeats
            };
        }

        [Fact]
        public void ReserveSeat_WhenSeatsAvailable_DecreasesAvailableSeatsByOne()
        {
            // Arrange
            var flight = CreateFlight(totalSeats: 2, availableSeats: 2);

            // Act
            flight.ReserveSeat();

            // Assert
            Assert.Equal(1, flight.AvailableSeats);
        }

        [Fact]
        public void ReserveSeat_WhenFullyBooked_ThrowsConflictException()
        {
            var flight = CreateFlight(totalSeats: 2, availableSeats: 0);

            var exception = Assert.Throws<ConflictException>(() => flight.ReserveSeat());

            Assert.Equal("Flight XQ1 has no available seats.", exception.Message);
            Assert.Equal(0, flight.AvailableSeats);
        }

        [Fact]
        public void ReleaseSeat_IncreasesAvailableSeatsByOne()
        {
            var flight = CreateFlight(totalSeats: 2, availableSeats: 0);

            flight.ReleaseSeat();

            Assert.Equal(1, flight.AvailableSeats);
        }

        [Theory]
        [InlineData(5, 3)]
        [InlineData(2, 0)]
        public void ChangeTotalSeats_WhenNotBelowSoldSeats_RecalculatesAvailableSeats(int newTotalSeats, int expectedAvailableSeats)
        {
            var flight = CreateFlight(totalSeats: 4, availableSeats: 2); // 2 seats sold

            flight.ChangeTotalSeats(newTotalSeats);

            Assert.Equal(newTotalSeats, flight.TotalSeats);
            Assert.Equal(expectedAvailableSeats, flight.AvailableSeats);
        }

        [Fact]
        public void ChangeTotalSeats_WhenBelowSoldSeats_ThrowsAndKeepsSeats()
        {
            var flight = CreateFlight(totalSeats: 4, availableSeats: 2); // 2 seats sold

            Assert.Throws<ConflictException>(() => flight.ChangeTotalSeats(1));

            Assert.Equal(4, flight.TotalSeats);
            Assert.Equal(2, flight.AvailableSeats);
        }
    }
}
