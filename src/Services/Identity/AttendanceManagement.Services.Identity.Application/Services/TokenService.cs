using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AttendanceManagement.Services.Identity.Application.Contracts;
using AttendanceManagement.Services.Identity.Domain.Entities;
using AttendanceManagement.Services.Identity.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AttendanceManagement.Services.Identity.Application.Services;

/// <summary>
/// Implements enterprise JWT generation and cryptographically secure refresh token handling.
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
        string? secretKey = _configuration["JwtSettings:Secret"];
        string? issuer = _configuration["JwtSettings:Issuer"];
        string? audience = _configuration["JwtSettings:Audience"];
        int expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "15");

        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
            throw new InvalidOperationException("JWT Secret key must be configured and be at least 32 characters long.");

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

    public RefreshToken GenerateRefreshToken(string jwtId, string userId)
    {
        // 64 bytes of cryptographically secure random bytes
        byte[] randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        string tokenValue = Convert.ToBase64String(randomBytes);
        int expiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        DateTime expiresAtUtc = DateTime.UtcNow.AddDays(expiryDays);

        return RefreshToken.Create(tokenValue, jwtId, userId, expiresAtUtc);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        string? secretKey = _configuration["JwtSettings:Secret"];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("JWT Secret key is not configured.");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateLifetime = false, // Critical: We must read claims from expired access token during refresh!
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new IdentityDomainException("Invalid JWT token format or security algorithm.");
        }

        return principal;
    }
}
