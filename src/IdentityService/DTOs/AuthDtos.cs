namespace IdentityService.DTOs;

/// <summary>
/// Credentials submitted during login.
/// </summary>
public record LoginRequestDto(string Email, string Password);

/// <summary>
/// Response payload containing JWT token pair and basic profile.
/// </summary>
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    Guid EmployeeId,
    string Email,
    string FullName,
    string Role,
    string Grade);

/// <summary>
/// Payload to register a new user and employee profile.
/// </summary>
public record RegisterEmployeeDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    int Grade,
    int Department,
    string Designation,
    DateTime JoiningDate,
    Guid? ReportingManagerId,
    string Role);

/// <summary>
/// Request payload to exchange an expired access token and refresh token for a new pair.
/// </summary>
public record RefreshTokenRequestDto(
    string AccessToken,
    string RefreshToken);

/// <summary>
/// Request payload to revoke an active refresh token upon user logout.
/// </summary>
public record RevokeTokenRequestDto(string RefreshToken);
