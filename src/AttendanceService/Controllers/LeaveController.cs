using System.Security.Claims;
using AttendanceService.DTOs;
using AttendanceService.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly IValidator<ApplyLeaveDto> _applyValidator;
    private readonly IValidator<UpdateLeaveRequestDto> _updateValidator;
    private readonly IValidator<ReviewLeaveDto> _reviewValidator;

    public LeaveController(
        ILeaveService leaveService,
        IValidator<ApplyLeaveDto> applyValidator,
        IValidator<UpdateLeaveRequestDto> updateValidator,
        IValidator<ReviewLeaveDto> reviewValidator)
    {
        _leaveService = leaveService;
        _applyValidator = applyValidator;
        _updateValidator = updateValidator;
        _reviewValidator = reviewValidator;
    }

    /// <summary>
    /// Applies for a new leave request. Validates sufficient balance against the append-only transaction ledger.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeaveRequestResponseDto>> Apply([FromBody] ApplyLeaveDto request, CancellationToken cancellationToken)
    {
        var validation = await _applyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _leaveService.ApplyLeaveAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates details of an unapproved leave request before manager review.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeaveRequestResponseDto>> Update(
        Guid id,
        [FromBody] UpdateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _leaveService.UpdateLeaveRequestAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a leave request by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves paginated leave history for an employee.
    /// </summary>
    [HttpGet("employee/{employeeId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<LeaveRequestResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LeaveRequestResponseDto>>> GetByEmployee(
        Guid employeeId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _leaveService.GetByEmployeeAsync(employeeId, pageIndex, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all pending leave requests requiring manager review.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveRequestResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestResponseDto>>> GetPending(CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetPendingRequestsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reviews a leave request (Approve or Reject).
    /// Approval records an atomic deduction entry in the leave ledger.
    /// </summary>
    [HttpPatch("{id:guid}/review")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestResponseDto>> Review(Guid id, [FromBody] ReviewLeaveDto review, CancellationToken cancellationToken)
    {
        var validation = await _reviewValidator.ValidateAsync(review, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        string? employeeClaim = User.FindFirstValue("employee_id");
        Guid approverEmployeeId = Guid.TryParse(employeeClaim, out var parsedGuid) ? parsedGuid : Guid.Empty;

        var result = await _leaveService.ReviewLeaveRequestAsync(id, approverEmployeeId, review, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancels a leave request. If previously approved, atomically records a refund into the ledger.
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
    /// Retrieves real-time calculated leave balance and recent ledger transactions for an employee.
    /// </summary>
    [HttpGet("balance/{employeeId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LeaveBalanceResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveBalanceResponseDto>> GetBalance(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetLeaveBalanceAsync(employeeId, cancellationToken);
        return Ok(result);
    }
}
