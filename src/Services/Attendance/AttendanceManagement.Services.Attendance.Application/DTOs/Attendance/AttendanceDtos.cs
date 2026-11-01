namespace AttendanceManagement.Services.Attendance.Application.DTOs.Attendance;

/// <summary>
/// Payload to log daily attendance for a project.
/// </summary>
public record CreateAttendanceDto(
    Guid EmployeeId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    string? Remarks);

/// <summary>
/// Payload to update an existing attendance record.
/// </summary>
public record UpdateAttendanceDto(
    Guid ProjectId,
    string ProjectName,
    string? Remarks);

/// <summary>
/// Standard response projection representing an attendance record.
/// </summary>
public record AttendanceResponseDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    string? Remarks,
    string Status,
    DateTime CreatedAtUtc);

/// <summary>
/// Aggregated daily attendance report.
/// </summary>
public record DailyAttendanceReportDto(
    DateOnly Date,
    int TotalPresent,
    int TotalAutoApproved,
    int TotalPendingApproval,
    IReadOnlyList<AttendanceResponseDto> Records);

/// <summary>
/// Aggregated weekly or monthly attendance summary report.
/// </summary>
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
