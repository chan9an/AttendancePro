namespace AttendanceManagement.Services.Attendance.Application.DTOs.Leave;

/// <summary>
/// Payload to submit a new leave request.
/// </summary>
public record ApplyLeaveDto(
    Guid EmployeeId,
    int LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason);

/// <summary>
/// Payload to update an unapproved leave request.
/// </summary>
public record UpdateLeaveRequestDto(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason);

/// <summary>
/// Payload submitted by manager to approve or reject a leave request.
/// </summary>
public record ReviewLeaveDto(
    bool IsApproved,
    string? Remarks);

/// <summary>
/// Response projection of a leave request.
/// </summary>
public record LeaveRequestResponseDto(
    Guid Id,
    Guid EmployeeId,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason,
    string Status,
    Guid? ApproverEmployeeId,
    string? ReviewRemarks,
    DateTime CreatedAtUtc);

/// <summary>
/// Real-time calculated leave balance and recent transaction breakdown.
/// </summary>
public record LeaveBalanceResponseDto(
    Guid EmployeeId,
    decimal AvailableBalanceDays,
    decimal TotalCreditedThisYear,
    decimal TotalConsumedThisYear,
    IReadOnlyList<LeaveLedgerEntryDto> RecentTransactions);

/// <summary>
/// Projection of an individual ledger entry row.
/// </summary>
public record LeaveLedgerEntryDto(
    Guid Id,
    string TransactionType,
    decimal Days,
    DateTime TransactionDateUtc,
    string Description,
    int AccountingYear,
    int AccountingMonth);

/// <summary>
/// Payload to trigger monthly leave credit processing.
/// </summary>
public record ProcessMonthlyCreditDto(
    int Year,
    int Month);
