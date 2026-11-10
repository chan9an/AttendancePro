namespace AttendanceManagement.Services.Attendance.Entities;

/// <summary>
/// Reference projection representing an employee's assignment to a project.
/// Satisfies project allocation within the Attendance microservice boundary.
/// Tracks assignment duration via StartDate and optional EndDate.
/// </summary>
public class ProjectAssignmentReference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Checks whether the project assignment is active and spans across the given date.
    /// </summary>
    public bool IsValidOnDate(DateOnly date)
    {
        return IsActive && StartDate <= date && (!EndDate.HasValue || EndDate.Value >= date);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
