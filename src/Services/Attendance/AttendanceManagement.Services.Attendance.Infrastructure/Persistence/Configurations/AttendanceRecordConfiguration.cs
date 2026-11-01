using AttendanceManagement.Services.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for AttendanceRecord entity.
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

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnType("datetime2");

        // Composite Unique Index: An employee cannot log more than one attendance record for the same calendar date!
        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique()
            .HasDatabaseName("IX_AttendanceRecords_EmployeeId_Date");

        // Index for querying project attendance reports
        builder.HasIndex(a => new { a.ProjectId, a.Date })
            .HasDatabaseName("IX_AttendanceRecords_ProjectId_Date");
    }
}
