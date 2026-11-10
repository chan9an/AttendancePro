using AttendanceService.DTOs;

namespace AttendanceService.Services;

public interface IAttendanceService
{
    Task<AttendanceResponseDto> CreateAttendanceAsync(CreateAttendanceDto request, CancellationToken cancellationToken = default);
    Task<AttendanceResponseDto> UpdateAttendanceAsync(Guid id, UpdateAttendanceDto request, CancellationToken cancellationToken = default);
    Task CancelAttendanceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AttendanceResponseDto> ApproveAttendanceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AttendanceResponseDto> RejectAttendanceAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<AttendanceResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<AttendanceResponseDto>> GetByEmployeeAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<DailyAttendanceReportDto> GetDailyReportAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<AttendanceSummaryReportDto> GetWeeklyReportAsync(Guid? employeeId, Guid? projectId, DateOnly weekStartDate, CancellationToken cancellationToken = default);
    Task<AttendanceSummaryReportDto> GetMonthlyReportAsync(Guid? employeeId, Guid? projectId, int year, int month, CancellationToken cancellationToken = default);
}
