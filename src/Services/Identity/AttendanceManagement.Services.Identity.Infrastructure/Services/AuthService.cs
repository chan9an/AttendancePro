using System.Security.Claims;
using AttendanceManagement.Services.Identity.Application.Common.Exceptions;
using AttendanceManagement.Services.Identity.Application.Contracts;
using AttendanceManagement.Services.Identity.Application.DTOs.Auth;
using AttendanceManagement.Services.Identity.Application.DTOs.Employee;
using AttendanceManagement.Services.Identity.Domain.Entities;
using AttendanceManagement.Services.Identity.Domain.Enums;
using AttendanceManagement.Services.Identity.Domain.Exceptions;
using AttendanceManagement.Services.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceManagement.Services.Identity.Infrastructure.Services;

/// <summary>
/// Implements enterprise authentication, registration, and refresh token rotation using ASP.NET Core Identity.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IEmployeeService _employeeService;
    private readonly IdentityAppDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IEmployeeService employeeService,
        IdentityAppDbContext context,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _employeeService = employeeService;
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Authentication failed: User with email {Email} not found.", request.Email);
            throw new IdentityDomainException("Invalid email or password.");
        }

        bool isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Authentication failed: Invalid credentials for user {Email}.", request.Email);
            throw new IdentityDomainException("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        string primaryRole = roles.FirstOrDefault() ?? "Employee";

        // Fetch employee profile
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id, cancellationToken);
        if (employee is null)
            throw new NotFoundException($"Employee profile for user {user.Email} not found.");

        if (employee.Status != EmploymentStatus.Active)
            throw new IdentityDomainException("Your employee account is inactive. Please contact HR.");

        // Generate Token Pair
        string accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole, employee);
        
        // Extract JTI from generated access token
        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        string jwtId = principal.Claims.First(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti).Value;

        var refreshToken = _tokenService.GenerateRefreshToken(jwtId, user.Id);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {Email} logged in successfully.", user.Email);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresAtUtc: refreshToken.ExpiresAtUtc,
            EmployeeId: employee.Id,
            Email: user.Email!,
            FullName: $"{employee.FirstName} {employee.LastName}".Trim(),
            Role: primaryRole,
            Grade: employee.Grade.ToString()
        );
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterEmployeeDto request, CancellationToken cancellationToken = default)
    {
        // 1. Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { nameof(request.Email), new[] { "An account with this email address already exists." } }
            });
        }

        // 2. Ensure Role exists
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(request.Role));
        }

        // 3. Create Identity User
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new ValidationException(errors);
        }

        await _userManager.AddToRoleAsync(user, request.Role);

        // 4. Create Employee Profile
        var createEmployeeDto = new CreateEmployeeDto(
            UserId: user.Id,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email,
            Grade: request.Grade,
            Department: request.Department,
            Designation: request.Designation,
            JoiningDate: request.JoiningDate,
            ReportingManagerId: request.ReportingManagerId
        );

        var employeeProfile = await _employeeService.CreateAsync(createEmployeeDto, cancellationToken);

        // Link EmployeeId back to ApplicationUser
        user.EmployeeId = employeeProfile.Id;
        await _userManager.UpdateAsync(user);

        // 5. Generate Auth Tokens
        var employee = await _context.Employees.FirstAsync(e => e.Id == employeeProfile.Id, cancellationToken);
        string accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, request.Role, employee);

        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        string jwtId = principal.Claims.First(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti).Value;

        var refreshToken = _tokenService.GenerateRefreshToken(jwtId, user.Id);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered and created employee {Email} with role {Role}", user.Email, request.Role);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresAtUtc: refreshToken.ExpiresAtUtc,
            EmployeeId: employee.Id,
            Email: user.Email,
            FullName: $"{employee.FirstName} {employee.LastName}".Trim(),
            Role: request.Role,
            Grade: employee.Grade.ToString()
        );
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Get principal from expired access token
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? jwtId = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(jwtId))
            throw new IdentityDomainException("Invalid access token claims.");

        // 2. Find the stored refresh token
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);
        if (storedToken is null)
            throw new IdentityDomainException("Refresh token does not exist.");

        // 3. Security Check: Replay Attack Detection
        if (storedToken.IsUsed)
        {
            _logger.LogWarning("Security Alert: Replay attack detected for Refresh Token {Token} of User {UserId}. Revoking all tokens.", storedToken.Token, userId);
            
            // Invalidate all tokens for this compromised user
            var allUserTokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync(cancellationToken);
            foreach (var token in allUserTokens)
            {
                token.Revoke();
            }
            await _context.SaveChangesAsync(cancellationToken);

            throw new IdentityDomainException("Invalid token operation. Potential security violation detected.");
        }

        if (storedToken.IsRevoked || storedToken.ExpiresAtUtc < DateTime.UtcNow)
            throw new IdentityDomainException("Refresh token is revoked or has expired.");

        if (storedToken.JwtId != jwtId)
            throw new IdentityDomainException("Refresh token does not match the provided access token.");

        // 4. Mark existing token as used (Token Rotation)
        storedToken.MarkAsUsed();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("Associated user account was not found.");

        var roles = await _userManager.GetRolesAsync(user);
        string primaryRole = roles.FirstOrDefault() ?? "Employee";

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id, cancellationToken);
        if (employee is null || employee.Status != EmploymentStatus.Active)
            throw new IdentityDomainException("Employee profile not found or inactive.");

        // 5. Generate fresh token pair
        string newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, primaryRole, employee);
        var newPrincipal = _tokenService.GetPrincipalFromExpiredToken(newAccessToken);
        string newJwtId = newPrincipal.Claims.First(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti).Value;

        var newRefreshToken = _tokenService.GenerateRefreshToken(newJwtId, user.Id);
        _context.RefreshTokens.Add(newRefreshToken);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Refreshed tokens successfully for User {UserId}", user.Id);

        return new AuthResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.Token,
            ExpiresAtUtc: newRefreshToken.ExpiresAtUtc,
            EmployeeId: employee.Id,
            Email: user.Email!,
            FullName: $"{employee.FirstName} {employee.LastName}".Trim(),
            Role: primaryRole,
            Grade: employee.Grade.ToString()
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);
        if (token is null)
            throw new NotFoundException("Refresh token was not found.");

        token.Revoke();
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Revoked refresh token for User {UserId}", token.UserId);
    }
}
