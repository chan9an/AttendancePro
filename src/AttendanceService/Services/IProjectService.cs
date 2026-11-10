using AttendanceService.DTOs;

namespace AttendanceService.Services;

public interface IProjectService
{
    // Project CRUD
    Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto request, CancellationToken cancellationToken = default);
    Task<ProjectResponseDto> UpdateProjectAsync(Guid id, UpdateProjectDto request, CancellationToken cancellationToken = default);
    Task<ProjectResponseDto> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectResponseDto>> GetAllProjectsAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task DeactivateProjectAsync(Guid id, CancellationToken cancellationToken = default);

    // Project Assignments
    Task<ProjectAssignmentResponseDto> AssignEmployeeAsync(AssignEmployeeProjectDto request, CancellationToken cancellationToken = default);
    Task RemoveAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task RemoveAssignmentAsync(Guid projectId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAssignmentResponseDto>> GetAssignmentsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAssignmentResponseDto>> GetAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
