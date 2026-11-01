using AttendanceManagement.Services.Attendance.Application.Common.Exceptions;
using AttendanceManagement.Services.Attendance.Application.Common.Models;
using AttendanceManagement.Services.Attendance.Application.Contracts;
using AttendanceManagement.Services.Attendance.Application.DTOs.Leave;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Attendance.Api.Controllers;

/// <summary>
/// Provides administrative endpoints for processing monthly leave credits and ledger balance adjustments.
/// </summary>
public class LeaveCreditController : ApiBaseController
{
    private readonly ILeaveCreditService _creditService;
    private readonly IValidator<ProcessMonthlyCreditDto> _creditValidator;

    public LeaveCreditController(
        ILeaveCreditService creditService,
        IValidator<ProcessMonthlyCreditDto> creditValidator)
    {
        _creditService = creditService;
        _creditValidator = creditValidator;
    }

    /// <summary>
    /// Processes monthly leave credits across all active employees for a specified year and month.
    /// Protected by database-level idempotency to prevent duplicate credit runs.
    /// Restricted to Administrators.
    /// </summary>
    [HttpPost("process-monthly")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessMonthlyCredits([FromBody] ProcessMonthlyCreditDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _creditValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        int processed = await _creditService.ProcessMonthlyCreditsAsync(request.Year, request.Month, cancellationToken);
        return OkResponse($"Successfully processed monthly leave credits for {processed} employees.", "Credit run completed.");
    }

    /// <summary>
    /// Records an administrative adjustment directly to an employee's leave ledger.
    /// Restricted to Administrators.
    /// </summary>
    [HttpPost("adjustment/{employeeId:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordAdjustment(Guid employeeId, [FromQuery] decimal days, [FromQuery] string reason, CancellationToken cancellationToken)
    {
        if (days == 0 || string.IsNullOrWhiteSpace(reason))
            return BadRequest(ApiResponse<string>.Failure("Days cannot be zero and reason is required."));

        await _creditService.RecordAdjustmentAsync(employeeId, days, reason, cancellationToken);
        return NoContent();
    }
}
