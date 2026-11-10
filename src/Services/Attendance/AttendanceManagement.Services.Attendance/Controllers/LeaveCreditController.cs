using AttendanceManagement.Services.Attendance.DTOs;
using AttendanceManagement.Services.Attendance.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Attendance.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class LeaveCreditController : ControllerBase
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
    /// </summary>
    [HttpPost("process-monthly")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> ProcessMonthlyCredits([FromBody] ProcessMonthlyCreditDto request, CancellationToken cancellationToken)
    {
        var validation = await _creditValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        int processed = await _creditService.ProcessMonthlyCreditsAsync(request.Year, request.Month, cancellationToken);
        return Ok($"Successfully processed monthly leave credits for {processed} employees.");
    }

    /// <summary>
    /// Records an administrative adjustment directly to an employee's leave ledger.
    /// </summary>
    [HttpPost("adjustment/{employeeId:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordAdjustment(
        Guid employeeId,
        [FromQuery] decimal days,
        [FromQuery] string reason,
        CancellationToken cancellationToken)
    {
        if (days == 0 || string.IsNullOrWhiteSpace(reason))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Adjustment",
                Detail = "Adjustment days cannot be zero and reason is required."
            });
        }

        await _creditService.RecordAdjustmentAsync(employeeId, days, reason, cancellationToken);
        return NoContent();
    }
}
