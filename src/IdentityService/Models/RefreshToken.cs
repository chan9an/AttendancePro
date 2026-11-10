namespace IdentityService.Models;

/// <summary>
/// Represents a cryptographic refresh token for extending expired JWT access tokens.
/// Only the SHA-256 hash of the token is stored in the database.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Cryptographic SHA-256 hash of the refresh token. The raw token is never persisted.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the paired JWT access token (Jti claim).
    /// </summary>
    public string JwtId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key identifier of the Identity user who owns this session.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Optimistic concurrency token to prevent race conditions during token rotation.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public bool IsActive => !IsUsed && !IsRevoked && DateTime.UtcNow < ExpiresAtUtc;

    public void MarkAsUsed()
    {
        IsUsed = true;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void Revoke()
    {
        IsRevoked = true;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }
}
