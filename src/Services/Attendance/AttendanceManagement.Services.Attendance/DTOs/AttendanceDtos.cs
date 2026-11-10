namespace AttendanceManagement.Services.Attendance.DTOs;

public record CreateAttendanceDto(
    Guid EmployeeId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    string? Remarks);

public record UpdateAttendanceDto(
    Guid ProjectId,
    string ProjectName,
    string? Remarks);

public record AttendanceResponseDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    string? Remarks,
    string Status,
    DateTime CreatedAtUtc);

public record DailyAttendanceReportDto(
    DateOnly Date,
    int TotalPresent,
    int TotalAutoApproved,
    int TotalPendingApproval,
    IReadOnlyList<AttendanceResponseDto> Records);

public record AttendanceSummaryReportDto(
    Guid? EmployeeId,
    Guid? ProjectId,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalWorkingDays,
    int ApprovedDays,
    int PendingDays,
    int RejectedDays,
    IReadOnlyList<AttendanceResponseDto> Details);
