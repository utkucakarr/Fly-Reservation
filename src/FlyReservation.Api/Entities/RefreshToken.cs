namespace FlyReservation.Api.Entities
{
    // A long-lived, random token that buys a new access token without asking for the password again.
    // Unlike the access token it is stored here, so it can be revoked
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // SHA-256 of the token. The token itself exists only in the client's cookie:
        // a leaked database must not hand out working sessions
        public string TokenHash { get; set; } = string.Empty;

        // Every token made from the same login shares this id (rotation: each refresh replaces the token).
        // When a token that was already used comes back, the whole family is revoked
        public Guid FamilyId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        // Set when the token is used (rotation), on logout, or when its family is revoked
        public DateTime? RevokedAt { get; set; }
    }
}
