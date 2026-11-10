using AttendanceService.Data;
using AttendanceService.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceService.Repositories;

/// <summary>
/// EF Core implementation of IAttendanceRepository.
/// Encapsulates all database queries and commands for attendance entities.
/// </summary>
public class AttendanceRepository : IAttendanceRepository
{
    private readonly AttendanceDbContext _context;

    public AttendanceRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> HasActiveAttendanceOnDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .AnyAsync(a => a.EmployeeId == employeeId &&
                           a.Date == date &&
                           a.Status != AttendanceStatus.Cancelled,
                      cancellationToken);
    }

    public async Task<bool> HasApprovedAttendanceInRangeAsync(Guid employeeId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .AnyAsync(a => a.EmployeeId == employeeId &&
                           a.Status == AttendanceStatus.Approved &&
                           a.Date >= startDate &&
                           a.Date <= endDate,
                      cancellationToken);
    }

    public async Task<AttendanceRecord> AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default)
    {
        await _context.AttendanceRecords.AddAsync(record, cancellationToken);
        return record;
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByEmployeePagedAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.Date)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .CountAsync(a => a.EmployeeId == employeeId, cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date == date && a.Status != AttendanceStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? employeeId,
        Guid? projectId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date >= startDate && a.Date <= endDate && a.Status != AttendanceStatus.Cancelled);

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(a => a.ProjectId == projectId.Value);
        }

        return await query.OrderBy(a => a.Date).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAllEmployeeIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
