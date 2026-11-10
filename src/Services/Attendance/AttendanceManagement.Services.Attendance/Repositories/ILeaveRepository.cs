using AttendanceManagement.Services.Attendance.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace AttendanceManagement.Services.Attendance.Repositories;

/// <summary>
/// Explicit repository interface for Leave requests, ledger entries, and grade entitlement data-access operations.
/// Includes transactional boundary and employee-level concurrency locking support.
/// </summary>
public interface ILeaveRepository
{
    // Leave Requests
    Task<LeaveRequest?> GetLeaveRequestByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasApprovedLeaveOnDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateOnly startDate, DateOnly endDate, Guid? excludeLeaveRequestId = null, CancellationToken cancellationToken = default);
    Task<decimal> GetPendingLeaveDaysAsync(Guid employeeId, Guid? excludeLeaveRequestId = null, CancellationToken cancellationToken = default);
    Task<LeaveRequest> AddLeaveRequestAsync(LeaveRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetLeaveRequestsByEmployeePagedAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetLeaveRequestsCountByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetPendingLeaveRequestsAsync(CancellationToken cancellationToken = default);

    // Append-Only Leave Ledger
    Task<decimal> GetAvailableBalanceAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<LeaveLedgerEntry> AddLedgerEntryAsync(LeaveLedgerEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveLedgerEntry>> GetLedgerTransactionsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> HasMonthlyCreditAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalCreditedDaysInYearAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);

    // Grade Entitlements
    Task<IReadOnlyList<GradeLeaveEntitlement>> GetGradeLeaveEntitlementsAsync(CancellationToken cancellationToken = default);
    Task AddGradeEntitlementsAsync(IEnumerable<GradeLeaveEntitlement> entitlements, CancellationToken cancellationToken = default);

    // Transaction & Concurrency Locking
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task AcquireEmployeeBalanceLockAsync(Guid employeeId, CancellationToken cancellationToken = default);

    // Save & Commit
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
