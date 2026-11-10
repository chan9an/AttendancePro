using AttendanceManagement.Services.Attendance.Entities;

namespace AttendanceManagement.Services.Attendance.Repositories;

/// <summary>
/// Explicit repository interface for Attendance data-access operations.
/// </summary>
public interface IAttendanceRepository
{
    Task<AttendanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAttendanceOnDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<bool> HasApprovedAttendanceInRangeAsync(Guid employeeId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<AttendanceRecord> AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByEmployeePagedAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, Guid? employeeId, Guid? projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetAllEmployeeIdsAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
