using AttendanceManagement.Services.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for GradeLeaveEntitlement entity.
/// </summary>
public class GradeLeaveEntitlementConfiguration : IEntityTypeConfiguration<GradeLeaveEntitlement>
{
    public void Configure(EntityTypeBuilder<GradeLeaveEntitlement> builder)
    {
        builder.ToTable("GradeLeaveEntitlements");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Grade)
            .IsRequired();

        builder.Property(g => g.GradeName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(g => g.MonthlyCreditDays)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(g => g.AnnualMaximumCap)
            .IsRequired()
            .HasPrecision(5, 2);

        // Unique index on Grade: One entitlement policy per grade
        builder.HasIndex(g => g.Grade)
            .IsUnique()
            .HasDatabaseName("IX_GradeLeaveEntitlements_Grade");
    }
}
