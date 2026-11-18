namespace FlyReservation.Api.Auth
{
    // Role names as constants, so a typo in [Authorize(Roles = ...)] is a compile error and not a silent 403
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
    }
}
