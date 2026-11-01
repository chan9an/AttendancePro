using AttendanceManagement.Services.Identity.Application.Common.Exceptions;
using AttendanceManagement.Services.Identity.Application.Common.Models;
using AttendanceManagement.Services.Identity.Application.Contracts;
using AttendanceManagement.Services.Identity.Application.DTOs.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Identity.Api.Controllers;

/// <summary>
/// Provides authentication endpoints for user login, employee registration, and refresh token rotation.
/// </summary>
public class AuthController : ApiBaseController
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly IValidator<RegisterEmployeeDto> _registerValidator;
    private readonly IValidator<RefreshTokenRequestDto> _refreshValidator;

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<RegisterEmployeeDto> registerValidator,
        IValidator<RefreshTokenRequestDto> refreshValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _refreshValidator = refreshValidator;
    }

    /// <summary>
    /// Authenticates an employee using corporate email and password.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT Access Token and Refresh Token pair with user profile info.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        var result = await _authService.LoginAsync(request, cancellationToken);
        return OkResponse(result, "Authentication successful.");
    }

    /// <summary>
    /// Registers a new employee user account and creates their corporate employee profile.
    /// Restricted to administrators.
    /// </summary>
    /// <param name="request">Employee registration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created authentication record and access tokens.</returns>
    [HttpPost("register")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterEmployeeDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        var result = await _authService.RegisterAsync(request, cancellationToken);
        return OkResponse(result, "Employee registered successfully.");
    }

    /// <summary>
    /// Exchanges an expired access token and active refresh token for a fresh token pair.
    /// </summary>
    /// <param name="request">Access and Refresh token pair.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fresh JWT Access Token and Refresh Token pair.</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _refreshValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        return OkResponse(result, "Token refreshed successfully.");
    }

    /// <summary>
    /// Revokes an active refresh token, effectively logging out the session.
    /// </summary>
    /// <param name="refreshToken">Refresh token value to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeToken([FromQuery] string refreshToken, CancellationToken cancellationToken)
    {
        await _authService.RevokeTokenAsync(refreshToken, cancellationToken);
        return NoContent();
    }
}
