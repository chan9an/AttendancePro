using AttendanceManagement.Services.Attendance.DTOs;
using AttendanceManagement.Services.Attendance.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagement.Services.Attendance.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IValidator<CreateProjectDto> _createValidator;
    private readonly IValidator<UpdateProjectDto> _updateValidator;
    private readonly IValidator<AssignEmployeeProjectDto> _assignValidator;

    public ProjectsController(
        IProjectService projectService,
        IValidator<CreateProjectDto> createValidator,
        IValidator<UpdateProjectDto> updateValidator,
        IValidator<AssignEmployeeProjectDto> assignValidator)
    {
        _projectService = projectService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _assignValidator = assignValidator;
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectResponseDto>> Create([FromBody] CreateProjectDto request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _projectService.CreateProjectAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves all projects.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectResponseDto>>> GetAll([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var result = await _projectService.GetAllProjectsAsync(activeOnly, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a project by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _projectService.GetProjectByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectResponseDto>> Update(Guid id, [FromBody] UpdateProjectDto request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _projectService.UpdateProjectAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deactivates a project.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _projectService.DeactivateProjectAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Assigns an employee to a project with StartDate and optional EndDate.
    /// Validates employee existence and active status against Identity Service.
    /// Prevents overlapping active assignments and preserves assignment history.
    /// </summary>
    [HttpPost("assignments")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(ProjectAssignmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProjectAssignmentResponseDto>> AssignEmployee(
        [FromBody] AssignEmployeeProjectDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _assignValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _projectService.AssignEmployeeAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deactivates a specific project assignment by its unique assignment identifier.
    /// Unambiguously removes an assignment even when multiple historical assignments exist for the employee and project.
    /// </summary>
    [HttpDelete("assignments/{assignmentId:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignmentById(Guid assignmentId, CancellationToken cancellationToken)
    {
        await _projectService.RemoveAssignmentByIdAsync(assignmentId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Removes / deactivates active assignments for an employee on a project.
    /// </summary>
    [HttpDelete("{projectId:guid}/assignments/{employeeId:guid}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignment(Guid projectId, Guid employeeId, CancellationToken cancellationToken)
    {
        await _projectService.RemoveAssignmentAsync(projectId, employeeId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieves all employee assignments for a specific project.
    /// </summary>
    [HttpGet("{projectId:guid}/assignments")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectAssignmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectAssignmentResponseDto>>> GetProjectAssignments(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _projectService.GetAssignmentsByProjectAsync(projectId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all project assignments for a specific employee.
    /// </summary>
    [HttpGet("employee/{employeeId:guid}/assignments")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectAssignmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectAssignmentResponseDto>>> GetEmployeeAssignments(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _projectService.GetAssignmentsByEmployeeAsync(employeeId, cancellationToken);
        return Ok(result);
    }
}
