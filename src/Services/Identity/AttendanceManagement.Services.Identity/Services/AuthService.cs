using System.Security.Claims;
using AttendanceManagement.Services.Identity.Common;
using AttendanceManagement.Services.Identity.Data;
using AttendanceManagement.Services.Identity.DTOs;
using AttendanceManagement.Services.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagement.Services.Identity.Services;

/// <summary>
/// Implements enterprise authentication, registration, refresh token rotation, and replay detection.
/// Enforces that only SHA-256 hashes of refresh tokens are stored in the database.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IdentityDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IdentityDbContext context,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Authentication failed: User with email {Email} not found.", request.Email);
            throw new DomainException("Invalid email or password.");
        }

        bool isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Authentication failed: Invalid credentials for user {Email}.", request.Email);
            throw new DomainException("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        string primaryRole = roles.FirstOrDefault() ?? "Employee";

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException($"Employee profile for user {user.Email} not found.");
        }

        if (employee.Status != EmploymentStatus.Active)
        {
            throw new DomainException("Your employee account is inactive. Please contact HR.");
        }

        // 1. Generate short-lived JWT access token
        string accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole, employee);

        // 2. Extract JTI claim from access token to cryptographically bind to the refresh token
        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        string jwtId = principal.Claims.First(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti).Value;

        // 3. Generate raw refresh token and its corresponding entity with SHA-256 hash
        var (rawRefreshToken, refreshTokenEntity) = _tokenService.GenerateRefreshToken(jwtId, user.Id);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in successfully.", user.Id);

        // 4. Return the raw token to the client (only the hash is stored in the database)
        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresAtUtc: refreshTokenEntity.ExpiresAtUtc,
            EmployeeId: employee.Id,
            Email: user.Email!,
            FullName: employee.FullName,
            Role: primaryRole,
            Grade: employee.Grade.ToString()
        );
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterEmployeeDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ConflictException($"An account with email '{request.Email}' already exists.");
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(request.Role));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new ValidationException(errors);
        }

        await _userManager.AddToRoleAsync(user, request.Role);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Grade = (EmployeeGrade)request.Grade,
            Department = (DepartmentType)request.Department,
            Designation = request.Designation.Trim(),
            JoiningDate = request.JoiningDate,
            ReportingManagerId = request.ReportingManagerId,
            Status = EmploymentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Employees.Add(employee);

        user.EmployeeId = employee.Id;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        string accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, request.Role, employee);

        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        string jwtId = principal.Claims.First(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti).Value;

        var (rawRefreshToken, refreshTokenEntity) = _tokenService.GenerateRefreshToken(jwtId, user.Id);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered employee {Email} with role {Role}", user.Email, request.Role);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresAtUtc: refreshTokenEntity.ExpiresAtUtc,
            EmployeeId: employee.Id,
            Email: user.Email,
            FullName: employee.FullName,
            Role: request.Role,
            Grade: employee.Grade.ToString()
        );
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Validate expired JWT access token: verifies signature, issuer, audience and signing key
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? jwtId = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(jwtId))
        {
            throw new DomainException("Invalid access token claims.");
        }

        // 2. Hash incoming raw token to find its cryptographic record in SQL Server
        string incomingTokenHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == incomingTokenHash, cancellationToken);
        if (storedToken is null)
        {
            throw new DomainException("Refresh token does not exist.");
        }

        // 3. Requirement 6: Verify stored refresh token belongs to the user identified by the JWT
        if (storedToken.UserId != userId)
        {
            _logger.LogWarning("Security Violation: Token owner mismatch. Token UserId {TokenUserId} != JWT UserId {JwtUserId}", storedToken.UserId, userId);
            throw new DomainException("Refresh token does not belong to the user identified by the access token.");
        }

        // 4. Requirement 7 & 9: Verify previous usage & Replay Attack Detection
        if (storedToken.IsUsed)
        {
            _logger.LogWarning("Security Alert: Replay attack detected for RefreshTokenId {TokenId} of User {UserId}. Revoking all tokens.", storedToken.Id, userId);

            // Invalidate all tokens for this compromised account
            var allUserTokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync(cancellationToken);
            foreach (var token in allUserTokens)
            {
                token.Revoke();
            }
            await _context.SaveChangesAsync(cancellationToken);

            throw new DomainException("Invalid token operation. Potential security violation detected.");
        }

        // 5. Requirement 7: Verify revocation and expiration
        if (storedToken.IsRevoked)
        {
            throw new DomainException("Refresh token is revoked.");
        }

        if (storedToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new DomainException("Refresh token has expired.");
        }

        // 6. Requirement 7: Verify JWT ID binding
        if (storedToken.JwtId != jwtId)
        {
            throw new DomainException("Refresh token does not match the provided access token.");
        }

        // 7. Verify user profile and active employment status
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new NotFoundException("Associated user account was not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        string primaryRole = roles.FirstOrDefault() ?? "Employee";

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id, cancellationToken);
        if (employee is null || employee.Status != EmploymentStatus.Active)
        {
            throw new DomainException("Employee profile not found or inactive.");
        }

        // 8. Requirement 8: Rotate token safely & prevent race conditions
        storedToken.MarkAsUsed();

        // Generate brand new access token and refresh token pair
        string newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole, employee);

        var newPrincipal = _tokenService.GetPrincipalFromExpiredToken(newAccessToken);
        string newJwtId = newPrincipal.Claims.First(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti).Value;

        var (newRawRefreshToken, newRefreshTokenEntity) = _tokenService.GenerateRefreshToken(newJwtId, user.Id);
        _context.RefreshTokens.Add(newRefreshTokenEntity);

        try
        {
            // Concurrency stamp on RefreshToken guarantees that only ONE concurrent request can mark it as used
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrent refresh collision detected for RefreshTokenId {TokenId} of User {UserId}.", storedToken.Id, userId);
            throw new DomainException("Concurrent refresh collision: this token was already rotated by an overlapping request.");
        }

        _logger.LogInformation("Refreshed tokens successfully for User {UserId}", user.Id);

        // 9. Return raw tokens to the client
        return new AuthResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRawRefreshToken,
            ExpiresAtUtc: newRefreshTokenEntity.ExpiresAtUtc,
            EmployeeId: employee.Id,
            Email: user.Email!,
            FullName: employee.FullName,
            Role: primaryRole,
            Grade: employee.Grade.ToString()
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        string tokenHash = _tokenService.HashToken(refreshToken);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
        if (token is null)
        {
            throw new NotFoundException("Refresh token was not found.");
        }

        token.Revoke();
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Revoked refresh token {TokenId} for User {UserId}", token.Id, token.UserId);
    }
}
