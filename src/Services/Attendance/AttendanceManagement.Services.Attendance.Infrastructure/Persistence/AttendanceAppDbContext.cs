using AttendanceManagement.Services.Attendance.Application.Common.Interfaces;
using AttendanceManagement.Services.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagement.Services.Attendance.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for Attendance & Leave Service.
/// </summary>
public class AttendanceAppDbContext : DbContext, IAttendanceAppDbContext
{
    public AttendanceAppDbContext(DbContextOptions<AttendanceAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveLedgerEntry> LeaveLedgerEntries => Set<LeaveLedgerEntry>();
    public DbSet<GradeLeaveEntitlement> GradeLeaveEntitlements => Set<GradeLeaveEntitlement>();
    public DbSet<ProjectAssignmentReference> ProjectAssignmentReferences => Set<ProjectAssignmentReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically discover and apply all IEntityTypeConfiguration<T> configurations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceAppDbContext).Assembly);
    }
}
