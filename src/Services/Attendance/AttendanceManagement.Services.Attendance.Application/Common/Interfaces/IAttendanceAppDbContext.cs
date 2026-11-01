using AttendanceManagement.Services.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagement.Services.Attendance.Application.Common.Interfaces;

/// <summary>
/// Abstraction for Attendance & Leave DbContext.
/// Enforces Clean Architecture by decoupling application workflows from concrete infrastructure.
/// </summary>
public interface IAttendanceAppDbContext
{
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveLedgerEntry> LeaveLedgerEntries { get; }
    DbSet<GradeLeaveEntitlement> GradeLeaveEntitlements { get; }
    DbSet<ProjectAssignmentReference> ProjectAssignmentReferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
