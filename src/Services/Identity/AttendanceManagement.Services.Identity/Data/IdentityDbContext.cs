using AttendanceManagement.Services.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagement.Services.Identity.Data;

/// <summary>
/// EF Core DbContext for Identity & Employee Service.
/// Inherits from IdentityDbContext to manage ASP.NET Identity tables alongside Employee and RefreshToken tables.
/// </summary>
public class IdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must call base.OnModelCreating first so ASP.NET Identity maps its tables (AspNetUsers, AspNetRoles, etc.)
        base.OnModelCreating(builder);

        // Discovers and applies all IEntityTypeConfiguration<T> classes in this project
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
