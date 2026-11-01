using AttendanceManagement.Services.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using AttendanceManagement.Services.Identity.Application.Common.Interfaces;

namespace AttendanceManagement.Services.Identity.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the Identity and Employee Microservice.
/// Inherits from IdentityDbContext to seamlessly integrate ASP.NET Core Identity tables
/// with custom enterprise Employee and RefreshToken tables.
/// </summary>
public class IdentityAppDbContext : IdentityDbContext<ApplicationUser>, IIdentityAppDbContext
{
    public IdentityAppDbContext(DbContextOptions<IdentityAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // 1. MUST call base.OnModelCreating first so ASP.NET Core Identity configures its schema (AspNetUsers, AspNetRoles, etc.)
        base.OnModelCreating(builder);

        // 2. Automatically discover and apply all IEntityTypeConfiguration<T> classes in this assembly.
        // This keeps OnModelCreating clean, modular, and prevents it from becoming a massive monolithic method.
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityAppDbContext).Assembly);
    }
}
