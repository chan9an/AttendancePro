using AttendanceManagement.Services.Attendance.Common;
using AttendanceManagement.Services.Attendance.Data;
using AttendanceManagement.Services.Attendance.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace AttendanceManagement.Services.Attendance.Repositories;

/// <summary>
/// EF Core implementation of ILeaveRepository.
/// Encapsulates all data-access operations for leave requests, append-only ledger entries, and grade policies.
/// Provides database transaction orchestration and SQL Server application-level locking (sp_getapplock)
/// to serialize employee-level leave approvals and protect against negative balances.
/// </summary>
public class LeaveRepository : ILeaveRepository
{
    private readonly AttendanceDbContext _context;

    public LeaveRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequest?> GetLeaveRequestByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<bool> HasApprovedLeaveOnDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .AnyAsync(l => l.EmployeeId == employeeId &&
                           l.Status == LeaveStatus.Approved &&
                           l.StartDate <= date &&
                           l.EndDate >= date,
                      cancellationToken);
    }

    public async Task<bool> HasOverlappingLeaveAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeLeaveRequestId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId &&
                        l.Status != LeaveStatus.Rejected &&
                        l.Status != LeaveStatus.Cancelled &&
                        l.StartDate <= endDate &&
                        l.EndDate >= startDate);

        if (excludeLeaveRequestId.HasValue)
        {
            query = query.Where(l => l.Id != excludeLeaveRequestId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<decimal> GetPendingLeaveDaysAsync(
        Guid employeeId,
        Guid? excludeLeaveRequestId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId &&
                        l.Status == LeaveStatus.Submitted &&
                        l.LeaveType != LeaveType.Unpaid);

        if (excludeLeaveRequestId.HasValue)
        {
            query = query.Where(l => l.Id != excludeLeaveRequestId.Value);
        }

        return await query.SumAsync(l => (decimal?)l.TotalDays, cancellationToken) ?? 0m;
    }

    public async Task<LeaveRequest> AddLeaveRequestAsync(LeaveRequest request, CancellationToken cancellationToken = default)
    {
        await _context.LeaveRequests.AddAsync(request, cancellationToken);
        return request;
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetLeaveRequestsByEmployeePagedAsync(
        Guid employeeId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetLeaveRequestsCountByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .CountAsync(l => l.EmployeeId == employeeId, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetPendingLeaveRequestsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Submitted)
            .OrderBy(l => l.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetAvailableBalanceAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveLedgerEntries
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .SumAsync(e => (decimal?)e.Days, cancellationToken) ?? 0m;
    }

    public async Task<LeaveLedgerEntry> AddLedgerEntryAsync(LeaveLedgerEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.LeaveLedgerEntries.AddAsync(entry, cancellationToken);
        return entry;
    }

    public async Task<IReadOnlyList<LeaveLedgerEntry>> GetLedgerTransactionsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveLedgerEntries
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.TransactionDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasMonthlyCreditAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveLedgerEntries
            .AsNoTracking()
            .AnyAsync(e => e.EmployeeId == employeeId &&
                           e.AccountingYear == year &&
                           e.AccountingMonth == month &&
                           e.TransactionType == LedgerTransactionType.MonthlyCredit,
                      cancellationToken);
    }

    public async Task<decimal> GetTotalCreditedDaysInYearAsync(Guid employeeId, int year, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveLedgerEntries
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId &&
                        e.AccountingYear == year &&
                        e.TransactionType == LedgerTransactionType.MonthlyCredit)
            .SumAsync(e => (decimal?)e.Days, cancellationToken) ?? 0m;
    }

    public async Task<IReadOnlyList<GradeLeaveEntitlement>> GetGradeLeaveEntitlementsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GradeLeaveEntitlements
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddGradeEntitlementsAsync(IEnumerable<GradeLeaveEntitlement> entitlements, CancellationToken cancellationToken = default)
    {
        await _context.GradeLeaveEntitlements.AddRangeAsync(entitlements, cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task AcquireEmployeeBalanceLockAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        string lockResource = $"LeaveBalance_{employeeId:N}";

        var resultParam = new SqlParameter
        {
            ParameterName = "@result",
            SqlDbType = SqlDbType.Int,
            Direction = ParameterDirection.Output
        };

        var resourceParam = new SqlParameter("@resource", lockResource);
        var timeoutParam = new SqlParameter("@timeout", 15000);

        // Execute sp_getapplock and capture return code
        await _context.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @timeout",
            new[] { resultParam, resourceParam, timeoutParam },
            cancellationToken);

        int returnCode = (int)(resultParam.Value ?? -999);
        if (returnCode < 0)
        {
            throw new ConflictException(
                $"Failed to acquire balance lock for Employee {employeeId} (sp_getapplock return code: {returnCode}). Please retry the operation.");
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
