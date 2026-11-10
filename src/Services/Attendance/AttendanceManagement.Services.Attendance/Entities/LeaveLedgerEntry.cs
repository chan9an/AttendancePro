namespace AttendanceManagement.Services.Attendance.Entities;

/// <summary>
/// Immutable append-only transaction ledger tracking employee leave credits and debits.
/// Every balance modification is recorded as an immutable row for auditability, concurrency safety, and historical integrity.
/// </summary>
public class LeaveLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid? LeaveRequestId { get; set; }
    public LedgerTransactionType TransactionType { get; set; }

    /// <summary>
    /// Transaction amount in days. Positive for credits/refunds (+); Negative for deductions (-).
    /// </summary>
    public decimal Days { get; set; }

    public DateTime TransactionDateUtc { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public int AccountingYear { get; set; }
    public int AccountingMonth { get; set; }

    public static LeaveLedgerEntry CreateMonthlyCredit(Guid employeeId, decimal days, int year, int month, string gradeName)
    {
        return new LeaveLedgerEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveRequestId = null,
            TransactionType = LedgerTransactionType.MonthlyCredit,
            Days = days,
            TransactionDateUtc = DateTime.UtcNow,
            Description = $"Monthly leave accrual for {year}-{month:D2} based on {gradeName} grade entitlement.",
            AccountingYear = year,
            AccountingMonth = month
        };
    }

    public static LeaveLedgerEntry CreateLeaveDeduction(Guid employeeId, Guid leaveRequestId, decimal days, int year, int month, LeaveType leaveType)
    {
        return new LeaveLedgerEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveRequestId = leaveRequestId,
            TransactionType = LedgerTransactionType.LeaveDeduction,
            Days = -Math.Abs(days), // Enforce negative deduction
            TransactionDateUtc = DateTime.UtcNow,
            Description = $"Leave deduction for approved {leaveType} request ({days} days).",
            AccountingYear = year,
            AccountingMonth = month
        };
    }

    public static LeaveLedgerEntry CreateCancellationRefund(Guid employeeId, Guid leaveRequestId, decimal days, int year, int month)
    {
        return new LeaveLedgerEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveRequestId = leaveRequestId,
            TransactionType = LedgerTransactionType.CancellationRefund,
            Days = Math.Abs(days), // Positive refund
            TransactionDateUtc = DateTime.UtcNow,
            Description = $"Leave refund for cancelled leave request ({days} days).",
            AccountingYear = year,
            AccountingMonth = month
        };
    }

    public static LeaveLedgerEntry CreateAdjustment(Guid employeeId, decimal days, int year, int month, string reason)
    {
        return new LeaveLedgerEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveRequestId = null,
            TransactionType = LedgerTransactionType.ManagerAdjustment,
            Days = days,
            TransactionDateUtc = DateTime.UtcNow,
            Description = $"Manager balance adjustment: {reason}",
            AccountingYear = year,
            AccountingMonth = month
        };
    }
}
