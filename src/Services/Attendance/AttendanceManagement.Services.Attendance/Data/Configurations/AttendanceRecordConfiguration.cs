using AttendanceManagement.Services.Attendance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Attendance.Data.Configurations;

/// <summary>
/// Fluent API configuration for AttendanceRecord entity.
/// Enforces filtered uniqueness: only one active (non-cancelled) attendance record is permitted per employee per calendar date.
/// If an attendance record is cancelled (Status = 4), an employee is permitted to re-log attendance for that date.
/// </summary>
public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EmployeeId)
            .IsRequired();

        builder.Property(a => a.Date)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(a => a.ProjectId)
            .IsRequired();

        builder.Property(a => a.ProjectName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Remarks)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.IsAutoApproved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnType("datetime2");

        // Composite Unique Filtered Index: An employee cannot log more than one active attendance record for the same calendar date.
        // Status == 4 (Cancelled) is excluded from the index, enabling employees to re-log attendance if their prior entry was cancelled.
        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique()
            .HasFilter("[Status] <> 4")
            .HasDatabaseName("IX_AttendanceRecords_EmployeeId_Date");

        builder.HasIndex(a => new { a.ProjectId, a.Date })
            .HasDatabaseName("IX_AttendanceRecords_ProjectId_Date");
    }
}
