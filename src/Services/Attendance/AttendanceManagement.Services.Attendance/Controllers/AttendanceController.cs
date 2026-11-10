using AttendanceManagement.Services.Attendance.DTOs;
using AttendanceManagement.Services.Attendance.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Attendance.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IValidator<CreateAttendanceDto> _createValidator;
    private readonly IValidator<UpdateAttendanceDto> _updateValidator;

    public AttendanceController(
        IAttendanceService attendanceService,
        IValidator<CreateAttendanceDto> createValidator,
        IValidator<UpdateAttendanceDto> updateValidator)
    {
        _attendanceService = attendanceService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Logs daily attendance for a project. Automatically evaluates auto-approval rules.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendanceResponseDto>> Create([FromBody] CreateAttendanceDto request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _attendanceService.CreateAttendanceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves a single attendance record by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves paginated attendance history for a specific employee.
    /// </summary>
    [HttpGet("employee/{employeeId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<AttendanceResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AttendanceResponseDto>>> GetByEmployee(
        Guid employeeId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _attendanceService.GetByEmployeeAsync(employeeId, pageIndex, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing attendance record.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponseDto>> Update(Guid id, [FromBody] UpdateAttendanceDto request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _attendanceService.UpdateAttendanceAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancels an attendance record.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _attendanceService.CancelAttendanceAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Approves an attendance record flagged as PendingApproval.
    /// </summary>
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(AttendanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponseDto>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.ApproveAttendanceAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Rejects an attendance record.
    /// </summary>
    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(AttendanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponseDto>> Reject(Guid id, [FromQuery] string reason, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.RejectAttendanceAsync(id, reason, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves daily attendance report across the organization for a given date.
    /// </summary>
    [HttpGet("reports/daily")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(DailyAttendanceReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyAttendanceReportDto>> GetDailyReport([FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetDailyReportAsync(date, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves weekly summary attendance report.
    /// </summary>
    [HttpGet("reports/weekly")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(AttendanceSummaryReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceSummaryReportDto>> GetWeeklyReport(
        [FromQuery] DateOnly weekStartDate,
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetWeeklyReportAsync(employeeId, projectId, weekStartDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves monthly summary attendance report.
    /// </summary>
    [HttpGet("reports/monthly")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(AttendanceSummaryReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceSummaryReportDto>> GetMonthlyReport(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetMonthlyReportAsync(employeeId, projectId, year, month, cancellationToken);
        return Ok(result);
    }
}
