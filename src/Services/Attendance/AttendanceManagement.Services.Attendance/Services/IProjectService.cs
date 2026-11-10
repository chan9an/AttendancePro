using AttendanceManagement.Services.Attendance.DTOs;

namespace AttendanceManagement.Services.Attendance.Services;

/// <summary>
/// Service interface for Project CRUD and Employee Project Assignment operations.
/// </summary>
public interface IProjectService
{
    Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto request, CancellationToken cancellationToken = default);
    Task<ProjectResponseDto> UpdateProjectAsync(Guid id, UpdateProjectDto request, CancellationToken cancellationToken = default);
    Task<ProjectResponseDto> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectResponseDto>> GetAllProjectsAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task DeactivateProjectAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProjectAssignmentResponseDto> AssignEmployeeAsync(AssignEmployeeProjectDto request, CancellationToken cancellationToken = default);
    Task RemoveAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task RemoveAssignmentAsync(Guid projectId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAssignmentResponseDto>> GetAssignmentsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAssignmentResponseDto>> GetAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
