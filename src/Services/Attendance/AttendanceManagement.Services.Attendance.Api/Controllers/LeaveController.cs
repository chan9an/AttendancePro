using System.Security.Claims;
using AttendanceManagement.Services.Attendance.Application.Common.Exceptions;
using AttendanceManagement.Services.Attendance.Application.Common.Models;
using AttendanceManagement.Services.Attendance.Application.Contracts;
using AttendanceManagement.Services.Attendance.Application.DTOs.Leave;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Attendance.Api.Controllers;

/// <summary>
/// Provides endpoints for employee leave requests, managerial approval workflows, and leave balances.
/// </summary>
public class LeaveController : ApiBaseController
{
    private readonly ILeaveService _leaveService;
    private readonly IValidator<ApplyLeaveDto> _applyValidator;
    private readonly IValidator<ReviewLeaveDto> _reviewValidator;

    public LeaveController(
        ILeaveService leaveService,
        IValidator<ApplyLeaveDto> applyValidator,
        IValidator<ReviewLeaveDto> reviewValidator)
    {
        _leaveService = leaveService;
        _applyValidator = applyValidator;
        _reviewValidator = reviewValidator;
    }

    /// <summary>
    /// Applies for a new leave request.
    /// Validates sufficient available balance from the transaction ledger.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply([FromBody] ApplyLeaveDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _applyValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        var result = await _leaveService.ApplyLeaveAsync(request, cancellationToken);
        return CreatedResponse(nameof(GetById), new { id = result.Id }, result, "Leave request submitted.");
    }

    /// <summary>
    /// Retrieves a leave request by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetByIdAsync(id, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Retrieves paginated leave history for a specific employee.
    /// </summary>
    [HttpGet("employee/{employeeId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LeaveRequestResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _leaveService.GetByEmployeeAsync(employeeId, pageIndex, pageSize, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Retrieves all pending leave requests requiring manager review.
    /// Restricted to Managers and Administrators.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LeaveRequestResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetPendingRequestsAsync(cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Reviews a leave request (Approve or Reject).
    /// Upon approval, creates an immutable LeaveDeduction row in the ledger.
    /// </summary>
    [HttpPatch("{id:guid}/review")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewLeaveDto review, CancellationToken cancellationToken)
    {
        var validationResult = await _reviewValidator.ValidateAsync(review, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        // Extract approver employeeId from claims or fallback
        string? employeeClaim = User.FindFirstValue("employee_id");
        Guid approverEmployeeId = Guid.TryParse(employeeClaim, out var parsedGuid) ? parsedGuid : Guid.Empty;

        var result = await _leaveService.ReviewLeaveRequestAsync(id, approverEmployeeId, review, cancellationToken);
        return OkResponse(result, review.IsApproved ? "Leave approved." : "Leave rejected.");
    }

    /// <summary>
    /// Cancels a leave request.
    /// If the leave was already approved, atomically credits an immutable refund into the ledger.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _leaveService.CancelLeaveRequestAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieves real-time leave balance and recent ledger transactions for an employee.
    /// </summary>
    [HttpGet("balance/{employeeId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<LeaveBalanceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetLeaveBalanceAsync(employeeId, cancellationToken);
        return OkResponse(result);
    }
}
