using System.Security.Claims;
using AttendanceManagement.Services.Identity.Domain.Entities;

namespace AttendanceManagement.Services.Identity.Application.Contracts;

/// <summary>
/// Service contract for generating and validating cryptographically secure JWT tokens and Refresh Tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT access token containing standard and customized enterprise claims.
    /// </summary>
    string GenerateAccessToken(string userId, string email, string role, Employee employee);

    /// <summary>
    /// Generates a cryptographically random 64-byte base64 encoded refresh token.
    /// </summary>
    RefreshToken GenerateRefreshToken(string jwtId, string userId);

    /// <summary>
    /// Extracts ClaimsPrincipal from an expired JWT without validating its lifetime.
    /// Used during refresh token exchange to securely resolve the caller's identity.
    /// </summary>
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
