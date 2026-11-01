using AttendanceManagement.Services.Identity.Domain.Exceptions;

namespace AttendanceManagement.Services.Identity.Domain.Entities;

/// <summary>
/// Represents a cryptographically secure refresh token used to renew expired JWT access tokens.
/// Implements token rotation and replay detection.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The cryptographically random refresh token string.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Identifier of the corresponding JWT access token (JTI claim) this refresh token was paired with.
    /// </summary>
    public string JwtId { get; private set; } = string.Empty;

    /// <summary>
    /// Foreign key reference to the Identity user account.
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether this token has already been exchanged for a new token pair.
    /// Used for token reuse / replay attack detection.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// Indicates whether this token was explicitly invalidated by logout or administrative action.
    /// </summary>
    public bool IsRevoked { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    protected RefreshToken()
    {
    }

    public static RefreshToken Create(
        string token,
        string jwtId,
        string userId,
        DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new IdentityDomainException("Token value is required.");

        if (string.IsNullOrWhiteSpace(jwtId))
            throw new IdentityDomainException("JwtId is required.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new IdentityDomainException("UserId is required.");

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new IdentityDomainException("Expiry date must be in the future.");

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            JwtId = jwtId,
            UserId = userId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    /// <summary>
    /// Marks the refresh token as used during token renewal.
    /// </summary>
    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new IdentityDomainException("Token has already been used. Possible replay attack detected.");

        IsUsed = true;
    }

    /// <summary>
    /// Revokes the refresh token, preventing further use.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
    }

    /// <summary>
    /// Checks if the token is currently active and eligible for token refresh.
    /// </summary>
    public bool IsActive => !IsUsed && !IsRevoked && DateTime.UtcNow < ExpiresAtUtc;
}
