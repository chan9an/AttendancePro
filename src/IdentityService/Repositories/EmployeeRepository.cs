using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Repositories;

/// <summary>
/// EF Core implementation of the Employee repository.
/// In accordance with transaction boundary design, SaveChangesAsync is called explicitly by the consuming service.
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly IdentityDbContext _context;

    public EmployeeRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Employee> query = _context.Employees.Include(e => e.ReportingManager);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetAllPagedAsync(
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.ReportingManager)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = email.ToLowerInvariant();
        return await _context.Employees.AnyAsync(e => e.Email == normalized, cancellationToken);
    }

    public async Task<bool> ActiveManagerExistsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees.AnyAsync(
            e => e.Id == managerId && e.Status == EmploymentStatus.Active,
            cancellationToken);
    }

    public async Task<Employee?> GetWithSubordinatesAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.Subordinates)
            .FirstOrDefaultAsync(e => e.Id == managerId, cancellationToken);
    }

    public Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _context.Employees.Add(employee);
        return Task.FromResult(employee);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
