using FluentValidation;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<UpdateEmployeeDto> _updateValidator;

    public EmployeesController(
        IEmployeeService employeeService,
        IValidator<UpdateEmployeeDto> updateValidator)
    {
        _employeeService = employeeService;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Retrieves paginated list of employees.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(PagedResult<EmployeeResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeResponseDto>>> GetAll(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetAllAsync(pageIndex, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves employee profile by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves employee profile by ASP.NET Identity UserId (used during service-to-service lookups).
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponseDto>> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates employee personal profile information.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(EmployeeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponseDto>> Update(Guid id, [FromBody] UpdateEmployeeDto request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _employeeService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Assigns or updates the organizational grade of an employee.
    /// </summary>
    [HttpPatch("{id:guid}/grade")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignGrade(Guid id, [FromBody] AssignGradeDto request, CancellationToken cancellationToken)
    {
        if (request.NewGrade < 1 || request.NewGrade > 3)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Grade",
                Detail = "Grade must be 1 (GradeC), 2 (GradeB), or 3 (GradeA)."
            });
        }

        await _employeeService.AssignGradeAsync(id, (EmployeeGrade)request.NewGrade, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Assigns or updates the reporting manager for an employee.
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
    /// Retrieves manager profile and all active direct reports.
    /// </summary>
    [HttpGet("hierarchy/{managerId:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ReportingHierarchyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportingHierarchyDto>> GetReportingHierarchy(Guid managerId, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetReportingHierarchyAsync(managerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deactivates an employee's employment status.
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
