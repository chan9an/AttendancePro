using AttendanceService.Common;

namespace AttendanceService.Models;

/// <summary>
/// Represents an employee's formal leave application and review lifecycle.
/// Protected by an optimistic ConcurrencyStamp to prevent simultaneous approvals or race conditions.
/// </summary>
public class LeaveRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Submitted;
    public Guid? ApproverEmployeeId { get; set; }
    public string? ReviewRemarks { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Concurrency token updated on each state mutation to prevent lost updates or simultaneous approvals.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public void Approve(Guid approverEmployeeId, string? remarks)
    {
        if (Status != LeaveStatus.Submitted)
        {
            throw new DomainException($"Only submitted requests can be approved. Current status: '{Status}'.");
        }

        Status = LeaveStatus.Approved;
        ApproverEmployeeId = approverEmployeeId != Guid.Empty ? approverEmployeeId : null;
        ReviewRemarks = remarks?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    public void Reject(Guid approverEmployeeId, string remarks)
    {
        if (Status != LeaveStatus.Submitted)
        {
            throw new DomainException($"Only submitted requests can be rejected. Current status: '{Status}'.");
        }

        if (string.IsNullOrWhiteSpace(remarks))
        {
            throw new DomainException("A reason must be provided when rejecting a leave request.");
        }

        Status = LeaveStatus.Rejected;
        ApproverEmployeeId = approverEmployeeId != Guid.Empty ? approverEmployeeId : null;
        ReviewRemarks = remarks.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    public void Cancel()
    {
        if (Status == LeaveStatus.Cancelled)
        {
            throw new DomainException("Leave request is already cancelled.");
        }

        if (Status == LeaveStatus.Rejected)
        {
            throw new DomainException("Cannot cancel a leave request that was already rejected.");
        }

        if (Status == LeaveStatus.Approved && StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new DomainException("Cannot cancel an approved leave that has already passed.");
        }

        Status = LeaveStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    public void Update(DateOnly startDate, DateOnly endDate, decimal totalDays, string reason)
    {
        if (Status != LeaveStatus.Submitted)
        {
            throw new DomainException("Cannot modify a leave request that has already been reviewed.");
        }

        if (endDate < startDate)
        {
            throw new DomainException("Leave end date cannot precede start date.");
        }

        if (totalDays <= 0)
        {
            throw new DomainException("Total leave days must be greater than zero.");
        }

        StartDate = startDate;
        EndDate = endDate;
        TotalDays = totalDays;
        Reason = reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }
}
