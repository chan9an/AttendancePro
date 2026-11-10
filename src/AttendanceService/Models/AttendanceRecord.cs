using AttendanceService.Common;

namespace AttendanceService.Models;

/// <summary>
/// Represents a daily project attendance log entry.
/// </summary>
public class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public AttendanceStatus Status { get; set; }
    public bool IsAutoApproved { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public void Approve()
    {
        if (Status != AttendanceStatus.PendingApproval)
        {
            throw new DomainException($"Only records with status 'PendingApproval' can be approved. Current status: '{Status}'.");
        }

        Status = AttendanceStatus.Approved;
        IsAutoApproved = false; // Manually approved by manager
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != AttendanceStatus.PendingApproval)
        {
            throw new DomainException($"Only records with status 'PendingApproval' can be rejected. Current status: '{Status}'.");
        }

        Status = AttendanceStatus.Rejected;
        Remarks = string.IsNullOrWhiteSpace(Remarks) ? $"Rejected: {reason}" : $"{Remarks} | Rejected: {reason}";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string? remarks, Guid projectId, string projectName, bool isAssignedToProject)
    {
        if (Status == AttendanceStatus.Cancelled)
        {
            throw new DomainException("Cannot update a cancelled attendance record.");
        }

        ProjectId = projectId;
        ProjectName = projectName;
        Remarks = remarks;
        Status = isAssignedToProject ? AttendanceStatus.Approved : AttendanceStatus.PendingApproval;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == AttendanceStatus.Cancelled)
        {
            throw new DomainException("Attendance record is already cancelled.");
        }

        Status = AttendanceStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
