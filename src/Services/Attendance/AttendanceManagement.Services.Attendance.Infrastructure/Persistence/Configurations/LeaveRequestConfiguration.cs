using AttendanceManagement.Services.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for LeaveRequest entity.
/// </summary>
public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.EmployeeId)
            .IsRequired();

        builder.Property(l => l.LeaveType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(l => l.StartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(l => l.EndDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(l => l.TotalDays)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(l => l.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(l => l.ApproverEmployeeId)
            .IsRequired(false);

        builder.Property(l => l.ReviewRemarks)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(l => l.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnType("datetime2");

        // Indexes for performance
        builder.HasIndex(l => new { l.EmployeeId, l.Status })
            .HasDatabaseName("IX_LeaveRequests_EmployeeId_Status");

        builder.HasIndex(l => new { l.StartDate, l.EndDate })
            .HasDatabaseName("IX_LeaveRequests_StartDate_EndDate");
    }
}
