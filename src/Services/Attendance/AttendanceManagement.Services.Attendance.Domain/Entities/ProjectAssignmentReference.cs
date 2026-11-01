using AttendanceManagement.Services.Attendance.Domain.Exceptions;

namespace AttendanceManagement.Services.Attendance.Domain.Entities;

/// <summary>
/// Reference projection representing an employee's assignment to a project.
/// Satisfies the project allocation requirement within the Attendance microservice boundary
/// without necessitating a dedicated Project microservice.
/// </summary>
public class ProjectAssignmentReference
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string ProjectName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    protected ProjectAssignmentReference()
    {
    }

    public static ProjectAssignmentReference Create(
        Guid employeeId,
        Guid projectId,
        string projectName)
    {
        if (employeeId == Guid.Empty)
            throw new AttendanceDomainException("Employee Id cannot be empty.");

        if (projectId == Guid.Empty)
            throw new AttendanceDomainException("Project Id cannot be empty.");

        if (string.IsNullOrWhiteSpace(projectName))
            throw new AttendanceDomainException("Project name is required.");

        return new ProjectAssignmentReference
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            ProjectId = projectId,
            ProjectName = projectName.Trim(),
            IsActive = true,
            AssignedAtUtc = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
