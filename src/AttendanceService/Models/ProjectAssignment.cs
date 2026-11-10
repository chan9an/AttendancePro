namespace AttendanceService.Models;

/// <summary>
/// Represents an employee's assignment to a project.
/// Tracks assignment duration via StartDate and optional EndDate.
/// </summary>
public class ProjectAssignment
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
