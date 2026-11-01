using AttendanceManagement.Services.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for ProjectAssignmentReference entity.
/// </summary>
public class ProjectAssignmentReferenceConfiguration : IEntityTypeConfiguration<ProjectAssignmentReference>
{
    public void Configure(EntityTypeBuilder<ProjectAssignmentReference> builder)
    {
        builder.ToTable("ProjectAssignmentReferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.EmployeeId)
            .IsRequired();

        builder.Property(p => p.ProjectId)
            .IsRequired();

        builder.Property(p => p.ProjectName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.AssignedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        // Composite unique index: An employee cannot have duplicate assignment rows for the same project
        builder.HasIndex(p => new { p.EmployeeId, p.ProjectId })
            .IsUnique()
            .HasDatabaseName("IX_ProjectAssignmentReferences_EmployeeId_ProjectId");
    }
}
