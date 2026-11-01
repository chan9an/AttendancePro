using AttendanceManagement.Services.Attendance.Domain.Enums;
using AttendanceManagement.Services.Attendance.Domain.Exceptions;

namespace AttendanceManagement.Services.Attendance.Domain.Entities;

/// <summary>
/// Represents a daily attendance entry logged by an employee for a specific project.
/// </summary>
public class AttendanceRecord
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Logical reference to the employee in Identity & Employee Service (No SQL foreign key).
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// The specific calendar date of attendance.
    /// </summary>
    public DateOnly Date { get; private set; }

    public Guid ProjectId { get; private set; }
    public string ProjectName { get; private set; } = string.Empty;
    public string? Remarks { get; private set; }

    public AttendanceStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    protected AttendanceRecord()
    {
    }

    /// <summary>
    /// Factory method creating an attendance record with automatic approval evaluation.
    /// </summary>
    public static AttendanceRecord Create(
        Guid id,
        Guid employeeId,
        DateOnly date,
        Guid projectId,
        string projectName,
        string? remarks,
        bool isAssignedToProject)
    {
        if (id == Guid.Empty)
            throw new AttendanceDomainException("Attendance Id cannot be empty.");

        if (employeeId == Guid.Empty)
            throw new AttendanceDomainException("Employee Id cannot be empty.");

        if (projectId == Guid.Empty)
            throw new AttendanceDomainException("Project Id cannot be empty.");

        if (string.IsNullOrWhiteSpace(projectName))
            throw new AttendanceDomainException("Project name is required.");

        // Invariant: Cannot log attendance for future dates
        if (date > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new AttendanceDomainException("Cannot log attendance for future dates.");

        // Auto-Approval Rule:
        // Automatically approved if assigned to project; otherwise flagged for manager approval.
        var status = isAssignedToProject
            ? AttendanceStatus.Approved
            : AttendanceStatus.PendingApproval;

        return new AttendanceRecord
        {
            Id = id,
            EmployeeId = employeeId,
            Date = date,
            ProjectId = projectId,
            ProjectName = projectName.Trim(),
            Remarks = remarks?.Trim(),
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Approves an attendance record that was pending review.
    /// </summary>
    public void Approve()
    {
        if (Status != AttendanceStatus.PendingApproval)
            throw new AttendanceDomainException($"Only records with status 'PendingApproval' can be approved. Current status: '{Status}'.");

        Status = AttendanceStatus.Approved;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects an attendance record.
    /// </summary>
    public void Reject(string reason)
    {
        if (Status != AttendanceStatus.PendingApproval)
            throw new AttendanceDomainException($"Only records with status 'PendingApproval' can be rejected. Current status: '{Status}'.");

        Status = AttendanceStatus.Rejected;
        Remarks = string.IsNullOrWhiteSpace(Remarks) ? $"Rejected: {reason}" : $"{Remarks} | Rejected: {reason}";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates attendance remarks and project association before payroll freeze.
    /// </summary>
    public void Update(string? remarks, Guid projectId, string projectName, bool isAssignedToProject)
    {
        if (Status == AttendanceStatus.Cancelled)
            throw new AttendanceDomainException("Cannot update a cancelled attendance record.");

        ProjectId = projectId;
        ProjectName = projectName;
        Remarks = remarks;
        Status = isAssignedToProject ? AttendanceStatus.Approved : AttendanceStatus.PendingApproval;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the attendance record.
    /// </summary>
    public void Cancel()
    {
        if (Status == AttendanceStatus.Cancelled)
            throw new AttendanceDomainException("Attendance record is already cancelled.");

        Status = AttendanceStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
