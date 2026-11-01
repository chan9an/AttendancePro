namespace AttendanceManagement.Services.Attendance.Domain.Enums;

/// <summary>
/// Categories of employee leave recognized by corporate policy.
/// </summary>
public enum LeaveType
{
    Annual = 1,
    Sick = 2,
    Casual = 3,
    Unpaid = 4
}

/// <summary>
/// Workflow status of an employee leave request.
/// </summary>
public enum LeaveStatus
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// Ledger entry classification for transactional leave balance tracking.
/// </summary>
public enum LedgerTransactionType
{
    /// <summary>
    /// Monthly accrual credited based on employee grade (+ units).
    /// </summary>
    MonthlyCredit = 1,

    /// <summary>
    /// Leave consumed upon leave request approval (- units).
    /// </summary>
    LeaveDeduction = 2,

    /// <summary>
    /// Leave refunded upon cancellation of an approved leave request (+ units).
    /// </summary>
    CancellationRefund = 3,

    /// <summary>
    /// Administrative or HR adjustment (+/- units).
    /// </summary>
    ManagerAdjustment = 4
}
