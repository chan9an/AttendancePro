using AttendanceManagement.Services.Attendance.Clients;
using AttendanceManagement.Services.Attendance.Common;
using AttendanceManagement.Services.Attendance.DTOs;
using AttendanceManagement.Services.Attendance.Entities;
using AttendanceManagement.Services.Attendance.Repositories;

namespace AttendanceManagement.Services.Attendance.Services;

/// <summary>
/// Implements Project CRUD and Employee Project Assignment management workflows.
/// Enforces valid assignment date ranges, prevents assignments to inactive projects,
/// prevents overlapping active assignments, and preserves full historical assignment records.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IIdentityIntegrationService _identityService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IProjectRepository projectRepository,
        IIdentityIntegrationService identityService,
        ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto request, CancellationToken cancellationToken = default)
    {
        bool nameExists = await _projectRepository.ProjectNameExistsAsync(request.Name, excludeId: null, cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"A project with name '{request.Name.Trim()}' already exists.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = request.Code?.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _projectRepository.AddProjectAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created project {ProjectId} ('{ProjectName}')", project.Id, project.Name);
        return MapToDto(project);
    }

    public async Task<ProjectResponseDto> UpdateProjectAsync(Guid id, UpdateProjectDto request, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetProjectByIdAsync(id, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }

        bool nameExists = await _projectRepository.ProjectNameExistsAsync(request.Name, excludeId: id, cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"Another project with name '{request.Name.Trim()}' already exists.");
        }

        project.Update(request.Name, request.Code, request.Description, request.IsActive);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated project {ProjectId} ('{ProjectName}')", project.Id, project.Name);
        return MapToDto(project);
    }

    public async Task<ProjectResponseDto> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetProjectByIdAsync(id, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }

        return MapToDto(project);
    }

    public async Task<IReadOnlyList<ProjectResponseDto>> GetAllProjectsAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetAllProjectsAsync(activeOnly, cancellationToken);
        return projects.Select(MapToDto).ToList();
    }

    public async Task DeactivateProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetProjectByIdAsync(id, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }

        project.Deactivate();
        await _projectRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deactivated project {ProjectId}", id);
    }

    public async Task<ProjectAssignmentResponseDto> AssignEmployeeAsync(AssignEmployeeProjectDto request, CancellationToken cancellationToken = default)
    {
        // 1. Assignment date range validation
        if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
        {
            throw new DomainException($"Assignment EndDate ({request.EndDate.Value}) cannot precede StartDate ({request.StartDate}).");
        }

        // 2. Identity Service Integration: Confirm employee existence and active employment status
        bool isEmployeeActive = await _identityService.IsEmployeeActiveAsync(request.EmployeeId, cancellationToken);
        if (!isEmployeeActive)
        {
            _logger.LogWarning("Project assignment rejected: Employee {EmployeeId} does not exist or is not active in Identity Service.", request.EmployeeId);
            throw new DomainException($"Employee {request.EmployeeId} does not exist or is not active in Identity Service.");
        }

        // 3. Verify target project exists and is active (prevention of assignments to inactive projects)
        var project = await _projectRepository.GetProjectByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), request.ProjectId);
        }

        if (!project.IsActive)
        {
            throw new DomainException($"Cannot assign employee to inactive project '{project.Name}'.");
        }

        // 4. Overlap Prevention: Check for overlapping active assignments for the same employee and project
        bool hasOverlap = await _projectRepository.HasOverlappingActiveAssignmentAsync(
            request.EmployeeId,
            request.ProjectId,
            request.StartDate,
            request.EndDate,
            excludeAssignmentId: null,
            cancellationToken);

        if (hasOverlap)
        {
            throw new ConflictException(
                $"An active assignment for Employee {request.EmployeeId} on Project '{project.Name}' already exists that overlaps with the specified date range ({request.StartDate} to {request.EndDate?.ToString() ?? "open"}).");
        }

        // 5. Preserve assignment history: Always create a new record rather than overwriting historical rows
        var assignment = new ProjectAssignmentReference
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            ProjectId = request.ProjectId,
            ProjectName = project.Name,
            IsActive = true,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AssignedAtUtc = DateTime.UtcNow
        };

        await _projectRepository.AddAssignmentAsync(assignment, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned Employee {EmployeeId} to Project {ProjectId} ('{ProjectName}') from {StartDate} to {EndDate}",
            assignment.EmployeeId, assignment.ProjectId, assignment.ProjectName, assignment.StartDate, assignment.EndDate);

        return MapToAssignmentDto(assignment);
    }

    public async Task RemoveAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await _projectRepository.GetAssignmentByIdAsync(assignmentId, cancellationToken);
        if (assignment is null)
        {
            throw new NotFoundException(nameof(ProjectAssignmentReference), assignmentId);
        }

        assignment.Deactivate();
        await _projectRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deactivated project assignment {AssignmentId} for Employee {EmployeeId}", assignmentId, assignment.EmployeeId);
    }

    public async Task RemoveAssignmentAsync(Guid projectId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        var assignment = await _projectRepository.GetAssignmentAsync(employeeId, projectId, cancellationToken);
        if (assignment is null)
        {
            throw new NotFoundException($"Active assignment for Employee {employeeId} on Project {projectId} was not found.");
        }

        assignment.Deactivate();
        await _projectRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deactivated active project assignment for Employee {EmployeeId} on Project {ProjectId}", employeeId, projectId);
    }

    public async Task<IReadOnlyList<ProjectAssignmentResponseDto>> GetAssignmentsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var assignments = await _projectRepository.GetAssignmentsByProjectAsync(projectId, cancellationToken);
        return assignments.Select(MapToAssignmentDto).ToList();
    }

    public async Task<IReadOnlyList<ProjectAssignmentResponseDto>> GetAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var assignments = await _projectRepository.GetAssignmentsByEmployeeAsync(employeeId, cancellationToken);
        return assignments.Select(MapToAssignmentDto).ToList();
    }

    private static ProjectResponseDto MapToDto(Project project)
    {
        return new ProjectResponseDto(
            Id: project.Id,
            Name: project.Name,
            Code: project.Code,
            Description: project.Description,
            IsActive: project.IsActive,
            CreatedAtUtc: project.CreatedAtUtc,
            UpdatedAtUtc: project.UpdatedAtUtc
        );
    }

    private static ProjectAssignmentResponseDto MapToAssignmentDto(ProjectAssignmentReference assignment)
    {
        return new ProjectAssignmentResponseDto(
            Id: assignment.Id,
            EmployeeId: assignment.EmployeeId,
            ProjectId: assignment.ProjectId,
            ProjectName: assignment.ProjectName,
            IsActive: assignment.IsActive,
            StartDate: assignment.StartDate,
            EndDate: assignment.EndDate,
            AssignedAtUtc: assignment.AssignedAtUtc
        );
    }
}
