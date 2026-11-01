namespace AttendanceManagement.Services.Attendance.Domain.Enums;

/// <summary>
/// State of a daily attendance record.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>
    /// Automatically approved by system rules (employee assigned to project, no conflicting leave).
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Pending manual manager review (e.g. employee logged hours on a project without prior assignment).
    /// </summary>
    PendingApproval = 2,

    /// <summary>
    /// Rejected by manager or compliance policy.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Cancelled by the employee before the payroll lock window.
    /// </summary>
    Cancelled = 4
}
