namespace AttendanceManagement.Services.Attendance.DTOs;

public record ApplyLeaveDto(
    Guid EmployeeId,
    int LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason);

public record UpdateLeaveRequestDto(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason);

public record ReviewLeaveDto(
    bool IsApproved,
    string? Remarks);

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

public record LeaveLedgerEntryDto(
    Guid Id,
    string TransactionType,
    decimal Days,
    DateTime TransactionDateUtc,
    string Description,
    int AccountingYear,
    int AccountingMonth);

public record LeaveBalanceResponseDto(
    Guid EmployeeId,
    decimal AvailableBalanceDays,
    decimal TotalCreditedThisYear,
    decimal TotalConsumedThisYear,
    IReadOnlyList<LeaveLedgerEntryDto> RecentTransactions);

public record ProcessMonthlyCreditDto(
    int Year,
    int Month);
