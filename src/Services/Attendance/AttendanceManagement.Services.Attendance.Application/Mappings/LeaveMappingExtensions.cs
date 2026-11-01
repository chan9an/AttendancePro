using AttendanceManagement.Services.Attendance.Application.DTOs.Leave;
using AttendanceManagement.Services.Attendance.Domain.Entities;

namespace AttendanceManagement.Services.Attendance.Application.Mappings;

/// <summary>
/// Static extension methods providing reflection-free compile-time mapping for Leave entities.
/// </summary>
public static class LeaveMappingExtensions
{
    public static LeaveRequestResponseDto ToResponseDto(this LeaveRequest request)
    {
        return new LeaveRequestResponseDto(
            Id: request.Id,
            EmployeeId: request.EmployeeId,
            LeaveType: request.LeaveType.ToString(),
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            TotalDays: request.TotalDays,
            Reason: request.Reason,
            Status: request.Status.ToString(),
            ApproverEmployeeId: request.ApproverEmployeeId,
            ReviewRemarks: request.ReviewRemarks,
            CreatedAtUtc: request.CreatedAtUtc
        );
    }

    public static IReadOnlyList<LeaveRequestResponseDto> ToResponseDtoList(this IEnumerable<LeaveRequest> requests)
    {
        return requests.Select(ToResponseDto).ToList();
    }

    public static LeaveLedgerEntryDto ToDto(this LeaveLedgerEntry entry)
    {
        return new LeaveLedgerEntryDto(
            Id: entry.Id,
            TransactionType: entry.TransactionType.ToString(),
            Days: entry.Days,
            TransactionDateUtc: entry.TransactionDateUtc,
            Description: entry.Description,
            AccountingYear: entry.AccountingYear,
            AccountingMonth: entry.AccountingMonth
        );
    }

    public static IReadOnlyList<LeaveLedgerEntryDto> ToDtoList(this IEnumerable<LeaveLedgerEntry> entries)
    {
        return entries.Select(ToDto).ToList();
    }
}
