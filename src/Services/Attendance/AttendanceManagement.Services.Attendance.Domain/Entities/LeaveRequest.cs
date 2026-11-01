using AttendanceManagement.Services.Attendance.Domain.Enums;
using AttendanceManagement.Services.Attendance.Domain.Exceptions;

namespace AttendanceManagement.Services.Attendance.Domain.Entities;

/// <summary>
/// Represents an employee's formal leave application and review lifecycle.
/// </summary>
public class LeaveRequest
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Logical reference to the employee requesting leave.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    public LeaveType LeaveType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    /// <summary>
    /// Total duration in work days (e.g. 1.0, 2.5).
    /// </summary>
    public decimal TotalDays { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    public LeaveStatus Status { get; private set; }

    /// <summary>
    /// Logical reference to the manager who approved or rejected the request.
    /// </summary>
    public Guid? ApproverEmployeeId { get; private set; }

    public string? ReviewRemarks { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    protected LeaveRequest()
    {
    }

    public static LeaveRequest Create(
        Guid id,
        Guid employeeId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        decimal totalDays,
        string reason)
    {
        if (id == Guid.Empty)
            throw new AttendanceDomainException("LeaveRequest Id cannot be empty.");

        if (employeeId == Guid.Empty)
            throw new AttendanceDomainException("Employee Id cannot be empty.");

        if (endDate < startDate)
            throw new AttendanceDomainException("Leave end date cannot precede start date.");

        if (totalDays <= 0)
            throw new AttendanceDomainException("Total leave days must be greater than zero.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new AttendanceDomainException("A valid reason for leave must be provided.");

        return new LeaveRequest
        {
            Id = id,
            EmployeeId = employeeId,
            LeaveType = leaveType,
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = totalDays,
            Reason = reason.Trim(),
            Status = LeaveStatus.Submitted,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Approves the leave request.
    /// </summary>
    public void Approve(Guid approverEmployeeId, string? remarks)
    {
        if (Status != LeaveStatus.Submitted)
            throw new AttendanceDomainException($"Only submitted requests can be approved. Current status: '{Status}'.");

        Status = LeaveStatus.Approved;
        ApproverEmployeeId = approverEmployeeId;
        ReviewRemarks = remarks?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the leave request.
    /// </summary>
    public void Reject(Guid approverEmployeeId, string remarks)
    {
        if (Status != LeaveStatus.Submitted)
            throw new AttendanceDomainException($"Only submitted requests can be rejected. Current status: '{Status}'.");

        if (string.IsNullOrWhiteSpace(remarks))
            throw new AttendanceDomainException("A reason must be provided when rejecting a leave request.");

        Status = LeaveStatus.Rejected;
        ApproverEmployeeId = approverEmployeeId;
        ReviewRemarks = remarks.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the leave request.
    /// </summary>
    public void Cancel()
    {
        if (Status == LeaveStatus.Cancelled)
            throw new AttendanceDomainException("Leave request is already cancelled.");

        if (Status == LeaveStatus.Rejected)
            throw new AttendanceDomainException("Cannot cancel a leave request that was already rejected.");

        Status = LeaveStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates details of a submitted leave request before it is approved.
    /// </summary>
    public void Update(DateOnly startDate, DateOnly endDate, decimal totalDays, string reason)
    {
        if (Status != LeaveStatus.Submitted)
            throw new AttendanceDomainException("Cannot modify a leave request that has already been reviewed.");

        if (endDate < startDate)
            throw new AttendanceDomainException("Leave end date cannot precede start date.");

        if (totalDays <= 0)
            throw new AttendanceDomainException("Total leave days must be greater than zero.");

        StartDate = startDate;
        EndDate = endDate;
        TotalDays = totalDays;
        Reason = reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
