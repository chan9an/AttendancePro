using AttendanceManagement.Services.Identity.Application.DTOs.Auth;

namespace AttendanceManagement.Services.Identity.Application.Contracts;

/// <summary>
/// Service contract for authentication operations, registration, and token rotation.
/// </summary>
public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RegisterAsync(RegisterEmployeeDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
