using AttendanceManagement.Services.Attendance.Application.Common.Exceptions;
using AttendanceManagement.Services.Attendance.Application.Common.Models;
using AttendanceManagement.Services.Attendance.Application.Contracts;
using AttendanceManagement.Services.Attendance.Application.DTOs.Attendance;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Attendance.Api.Controllers;

/// <summary>
/// Provides endpoints for daily attendance logging, auto-approval queries, manager reviews, and reporting.
/// </summary>
public class AttendanceController : ApiBaseController
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
    /// Logs daily attendance for a project.
    /// Evaluates auto-approval rules: Auto-approves if employee is assigned to the project and has no approved leave.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        var result = await _attendanceService.CreateAttendanceAsync(request, cancellationToken);
        return CreatedResponse(nameof(GetById), new { id = result.Id }, result, "Attendance logged successfully.");
    }

    /// <summary>
    /// Retrieves a single attendance record by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetByIdAsync(id, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Retrieves paginated attendance history for a specific employee.
    /// </summary>
    [HttpGet("employee/{employeeId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AttendanceResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEmployee(Guid employeeId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _attendanceService.GetByEmployeeAsync(employeeId, pageIndex, pageSize, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Updates an existing attendance record.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttendanceDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        var result = await _attendanceService.UpdateAttendanceAsync(id, request, cancellationToken);
        return OkResponse(result, "Attendance record updated.");
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
    /// Approves an attendance record that was flagged as PendingApproval.
    /// Restricted to Managers and Administrators.
    /// </summary>
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.ApproveAttendanceAsync(id, cancellationToken);
        return OkResponse(result, "Attendance approved successfully.");
    }

    /// <summary>
    /// Rejects an attendance record.
    /// Restricted to Managers and Administrators.
    /// </summary>
    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, [FromQuery] string reason, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.RejectAttendanceAsync(id, reason, cancellationToken);
        return OkResponse(result, "Attendance rejected.");
    }

    /// <summary>
    /// Retrieves daily attendance report across the organization for a given date.
    /// </summary>
    [HttpGet("reports/daily")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<DailyAttendanceReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyReport([FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetDailyReportAsync(date, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Retrieves weekly summary attendance report filtered optionally by employee or project.
    /// </summary>
    [HttpGet("reports/weekly")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSummaryReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklyReport([FromQuery] DateOnly weekStartDate, [FromQuery] Guid? employeeId, [FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetWeeklyReportAsync(employeeId, projectId, weekStartDate, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Retrieves monthly summary attendance report filtered optionally by employee or project.
    /// </summary>
    [HttpGet("reports/monthly")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSummaryReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] int year, [FromQuery] int month, [FromQuery] Guid? employeeId, [FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetMonthlyReportAsync(employeeId, projectId, year, month, cancellationToken);
        return OkResponse(result);
    }
}
