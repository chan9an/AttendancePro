namespace AttendanceService.Models;

/// <summary>
/// Status of a daily project attendance record.
/// </summary>
public enum AttendanceStatus
{
    Approved = 1,
    PendingApproval = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// Recognized leave categories.
/// </summary>
public enum LeaveType
{
    Annual = 1,
    Sick = 2,
    Casual = 3,
    Unpaid = 4
}

/// <summary>
/// Status lifecycle of an employee leave application.
/// </summary>
public enum LeaveStatus
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// Transaction type for immutable leave ledger records.
/// </summary>
public enum LedgerTransactionType
{
    MonthlyCredit = 1,      // Accrual credited based on grade (+ units)
    LeaveDeduction = 2,     // Debited upon approval (- units)
    CancellationRefund = 3, // Refunded upon approved cancellation (+ units)
    ManagerAdjustment = 4   // Administrative adjustment (+/- units)
}
