using IdentityService.Models;

namespace IdentityService.Repositories;

/// <summary>
/// Explicit repository interface for Employee data-access operations.
/// </summary>
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken cancellationToken = default);
    Task<Employee?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetAllPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ActiveManagerExistsAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task<Employee?> GetWithSubordinatesAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
