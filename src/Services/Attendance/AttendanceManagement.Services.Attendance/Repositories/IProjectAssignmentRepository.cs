using AttendanceManagement.Services.Attendance.Entities;

namespace AttendanceManagement.Services.Attendance.Repositories;

/// <summary>
/// Explicit repository interface for Projects and ProjectAssignmentReference data-access operations.
/// </summary>
public interface IProjectAssignmentRepository
{
    // Projects
    Task<Project?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetAllProjectsAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<bool> ProjectNameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Project> AddProjectAsync(Project project, CancellationToken cancellationToken = default);

    // Project Assignments
    Task<bool> HasValidAssignmentOnDateAsync(Guid employeeId, Guid projectId, DateOnly date, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingActiveAssignmentAsync(Guid employeeId, Guid projectId, DateOnly startDate, DateOnly? endDate, Guid? excludeAssignmentId = null, CancellationToken cancellationToken = default);
    Task<ProjectAssignmentReference?> GetAssignmentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectAssignmentReference?> GetAssignmentAsync(Guid employeeId, Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAssignmentReference>> GetAssignmentsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAssignmentReference>> GetAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetActiveEmployeeIdsAsync(CancellationToken cancellationToken = default);
    Task<ProjectAssignmentReference> AddAssignmentAsync(ProjectAssignmentReference assignment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
