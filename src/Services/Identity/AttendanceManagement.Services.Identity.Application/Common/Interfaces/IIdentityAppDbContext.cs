using AttendanceManagement.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagement.Services.Identity.Application.Common.Interfaces;

/// <summary>
/// Abstraction for Identity & Employee DbContext.
/// Decouples the Application layer from concrete EF Core Infrastructure.
/// </summary>
public interface IIdentityAppDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
