using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityService.Common;
using IdentityService.Models;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Services;

/// <summary>
/// Implements JWT token generation, cryptographically secure refresh token creation,
/// SHA-256 token hashing, and expired token claim inspection.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(string userId, string email, string role, Employee employee)
    {
        string secretKey = _configuration["JwtSettings:Secret"] 
            ?? throw new InvalidOperationException("JwtSettings:Secret configuration is missing.");
        string issuer = _configuration["JwtSettings:Issuer"] ?? "AttendanceManagement.IdentityService";
        string audience = _configuration["JwtSettings:Audience"] ?? "AttendanceManagement.Clients";
        int expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "15");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role),
            new("employee_id", employee.Id.ToString()),
            new("first_name", employee.FirstName),
            new("last_name", employee.LastName),
            new("grade", employee.Grade.ToString()),
            new("department", employee.Department.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public (string RawToken, RefreshToken Entity) GenerateRefreshToken(string jwtId, string userId)
    {
        // 1. Generate 64 cryptographically secure random bytes (512 bits)
        byte[] randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Raw token returned to the client
        string rawToken = Convert.ToBase64String(randomBytes);

        // 2. Compute SHA-256 hash to persist in database (raw token is never saved)
        string tokenHash = HashToken(rawToken);
        int expiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            JwtId = jwtId,
            UserId = userId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(expiryDays),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        return (rawToken, entity);
    }

    public string HashToken(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        string secretKey = _configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret configuration is missing.");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateLifetime = false, // Allows extracting claims from expired access token during renewal
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        // Verify cryptographic algorithm matches HMAC-SHA256
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new DomainException("Invalid JWT token format or signing algorithm.");
        }

        return principal;
    }
}
