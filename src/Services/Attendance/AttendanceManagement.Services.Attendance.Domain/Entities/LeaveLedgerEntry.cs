using AttendanceManagement.Services.Attendance.Domain.Enums;
using AttendanceManagement.Services.Attendance.Domain.Exceptions;

namespace AttendanceManagement.Services.Attendance.Domain.Entities;

/// <summary>
/// Immutable double-entry style financial transaction ledger tracking employee leave credits and debits.
/// Every balance modification is recorded as an immutable row for compliance, auditability, and concurrency safety.
/// </summary>
public class LeaveLedgerEntry
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Logical reference to the employee owning the leave balance.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Optional foreign key to the LeaveRequest that caused this debit or refund.
    /// </summary>
    public Guid? LeaveRequestId { get; private set; }

    /// <summary>
    /// Classification of transaction: MonthlyCredit, LeaveDeduction, CancellationRefund, ManagerAdjustment.
    /// </summary>
    public LedgerTransactionType TransactionType { get; private set; }

    /// <summary>
    /// Transaction amount in days.
    /// Positive for credits and refunds (+); Negative for consumption/deductions (-).
    /// </summary>
    public decimal Days { get; private set; }

    public DateTime TransactionDateUtc { get; private set; }
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Calendar accounting year (e.g. 2026).
    /// </summary>
    public int AccountingYear { get; private set; }

    /// <summary>
    /// Calendar accounting month (1 - 12).
    /// </summary>
    public int AccountingMonth { get; private set; }

    protected LeaveLedgerEntry()
    {
    }

    /// <summary>
    /// Factory method to record a monthly leave entitlement credit.
    /// </summary>
    public static LeaveLedgerEntry CreateMonthlyCredit(
        Guid employeeId,
        decimal days,
        int year,
        int month,
        string gradeName)
    {
        if (employeeId == Guid.Empty)
            throw new AttendanceDomainException("Employee Id cannot be empty.");

        if (days <= 0)
            throw new AttendanceDomainException("Monthly credit days must be greater than zero.");

        if (month < 1 || month > 12)
            throw new AttendanceDomainException("Invalid calendar month.");

        return new LeaveLedgerEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveRequestId = null,
            TransactionType = LedgerTransactionType.MonthlyCredit,
            Days = days, // Positive credit
            TransactionDateUtc = DateTime.UtcNow,
            Description = $"Monthly leave accrual for {year}-{month:D2} based on {gradeName} grade entitlement.",
            AccountingYear = year,
            AccountingMonth = month
        };
    }

    /// <summary>
    /// Factory method to record a leave deduction when a leave request is approved.
    /// </summary>
    public static LeaveLedgerEntry CreateLeaveDeduction(
        Guid employeeId,
        Guid leaveRequestId,
        decimal days,
        int year,
        int month,
        LeaveType leaveType)
    {
        if (employeeId == Guid.Empty)
            throw new AttendanceDomainException("Employee Id cannot be empty.");

        if (leaveRequestId == Guid.Empty)
            throw new AttendanceDomainException("LeaveRequest Id cannot be empty.");

        if (days <= 0)
            throw new AttendanceDomainException("Deduction days must be greater than zero.");

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

    /// <summary>
    /// Factory method to record a leave refund when an approved leave request is cancelled.
    /// </summary>
    public static LeaveLedgerEntry CreateCancellationRefund(
        Guid employeeId,
        Guid leaveRequestId,
        decimal days,
        int year,
        int month)
    {
        if (employeeId == Guid.Empty)
            throw new AttendanceDomainException("Employee Id cannot be empty.");

        if (days <= 0)
            throw new AttendanceDomainException("Refund days must be greater than zero.");

        return new LeaveLedgerEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveRequestId = leaveRequestId,
            TransactionType = LedgerTransactionType.CancellationRefund,
            Days = Math.Abs(days), // Positive credit
            TransactionDateUtc = DateTime.UtcNow,
            Description = $"Leave refund for cancelled leave request ({days} days).",
            AccountingYear = year,
            AccountingMonth = month
        };
    }

    /// <summary>
    /// Factory method to record an administrative adjustment by HR or management.
    /// </summary>
    public static LeaveLedgerEntry CreateAdjustment(
        Guid employeeId,
        decimal days,
        int year,
        int month,
        string reason)
    {
        if (employeeId == Guid.Empty)
            throw new AttendanceDomainException("Employee Id cannot be empty.");

        if (days == 0)
            throw new AttendanceDomainException("Adjustment days cannot be zero.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new AttendanceDomainException("A reason must be provided for manual ledger balance adjustments.");

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
