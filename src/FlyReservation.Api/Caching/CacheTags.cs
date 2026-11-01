namespace FlyReservation.Api.Caching
{
    public static class CacheTags
    {
        // Every cached flight search carries this tag, so one call can invalidate all of them
        public const string FlightSearch = "flight-search";
    }
}
