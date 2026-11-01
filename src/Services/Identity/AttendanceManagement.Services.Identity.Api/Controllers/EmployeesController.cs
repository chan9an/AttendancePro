using AttendanceManagement.Services.Identity.Application.Common.Exceptions;
using AttendanceManagement.Services.Identity.Application.Common.Models;
using AttendanceManagement.Services.Identity.Application.Contracts;
using AttendanceManagement.Services.Identity.Application.DTOs.Employee;
using AttendanceManagement.Services.Identity.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Identity.Api.Controllers;

/// <summary>
/// Provides employee management endpoints including profiles, reporting hierarchies, and grade updates.
/// </summary>
public class EmployeesController : ApiBaseController
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<CreateEmployeeDto> _createValidator;
    private readonly IValidator<UpdateEmployeeDto> _updateValidator;

    public EmployeesController(
        IEmployeeService employeeService,
        IValidator<CreateEmployeeDto> createValidator,
        IValidator<UpdateEmployeeDto> updateValidator)
    {
        _employeeService = employeeService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Retrieves paginated list of employees.
    /// Accessible to Administrators and Managers.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EmployeeResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetAllAsync(pageIndex, pageSize, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Retrieves a single employee profile by employee identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetByIdAsync(id, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Retrieves an employee profile by their ASP.NET Core Identity user ID.
    /// Useful during internal microservice identity resolution.
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetByUserIdAsync(userId, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Updates employee personal profile information.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ToDictionary());

        var result = await _employeeService.UpdateAsync(id, request, cancellationToken);
        return OkResponse(result, "Employee profile updated successfully.");
    }

    /// <summary>
    /// Assigns or updates the organizational grade of an employee.
    /// Restricted to Administrators.
    /// </summary>
    [HttpPatch("{id:guid}/grade")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignGrade(Guid id, [FromBody] AssignGradeDto request, CancellationToken cancellationToken)
    {
        if (request.NewGrade < 1 || request.NewGrade > 3)
            return BadRequest(ApiResponse<string>.Failure("Invalid grade specification. Grade must be 1 (C), 2 (B), or 3 (A)."));

        await _employeeService.AssignGradeAsync(id, (EmployeeGrade)request.NewGrade, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Assigns or changes the direct reporting manager for an employee.
    /// Restricted to Administrators.
    /// </summary>
    [HttpPatch("{id:guid}/manager")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignReportingManager(Guid id, [FromBody] AssignManagerDto request, CancellationToken cancellationToken)
    {
        await _employeeService.AssignReportingManagerAsync(id, request.ManagerId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieves the reporting hierarchy and all active direct reports under a specified manager.
    /// </summary>
    [HttpGet("hierarchy/{managerId:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ApiResponse<ReportingHierarchyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportingHierarchy(Guid managerId, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetReportingHierarchyAsync(managerId, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>
    /// Deactivates an employee's employment status.
    /// Restricted to Administrators.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _employeeService.DeactivateEmployeeAsync(id, cancellationToken);
        return NoContent();
    }
}
