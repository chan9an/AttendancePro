using AttendanceManagement.Services.Attendance.Entities;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagement.Services.Attendance.Data;

/// <summary>
/// EF Core DbContext for the Attendance & Leave Management Service.
/// </summary>
public class AttendanceDbContext : DbContext
{
    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options)
    {
    }

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveLedgerEntry> LeaveLedgerEntries => Set<LeaveLedgerEntry>();
    public DbSet<GradeLeaveEntitlement> GradeLeaveEntitlements => Set<GradeLeaveEntitlement>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAssignmentReference> ProjectAssignmentReferences => Set<ProjectAssignmentReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceDbContext).Assembly);
    }
}
