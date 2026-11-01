namespace AttendanceManagement.Services.Identity.Application.DTOs.Auth;

/// <summary>
/// Credentials payload submitted during user authentication.
/// </summary>
public record LoginRequestDto(string Email, string Password);

/// <summary>
/// Successful authentication payload containing tokens and user context.
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
/// Request payload to register a new employee user account.
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
/// Payload to exchange an active refresh token for a fresh JWT token pair.
/// </summary>
public record RefreshTokenRequestDto(
    string AccessToken,
    string RefreshToken);
