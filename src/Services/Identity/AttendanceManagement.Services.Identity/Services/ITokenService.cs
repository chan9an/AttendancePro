using System.Security.Claims;
using AttendanceManagement.Services.Identity.Entities;

namespace AttendanceManagement.Services.Identity.Services;

public interface ITokenService
{
    /// <summary>
    /// Generates a signed, short-lived JWT access token with role and employee profile claims.
    /// </summary>
    string GenerateAccessToken(string userId, string email, string role, Employee employee);

    /// <summary>
    /// Generates a cryptographically secure random refresh token.
    /// Returns the raw token string (for the client) and the entity with only the SHA-256 hash (for the database).
    /// </summary>
    (string RawToken, RefreshToken Entity) GenerateRefreshToken(string jwtId, string userId);

    /// <summary>
    /// Computes the SHA-256 hash of a refresh token.
    /// </summary>
    string HashToken(string token);

    /// <summary>
    /// Validates signature, issuer, audience and signing key of an expired JWT to safely extract claims.
    /// </summary>
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
