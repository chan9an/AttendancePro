using AttendanceService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceService.Data.Configurations;

/// <summary>
/// Fluent API configuration for ProjectAssignment entity.
/// Maps to existing database table 'ProjectAssignmentReferences' to preserve database schema integrity.
/// Preserves complete assignment history by allowing multiple non-overlapping historical records per employee/project.
/// </summary>
public class ProjectAssignmentConfiguration : IEntityTypeConfiguration<ProjectAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectAssignment> builder)
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

        builder.Property(p => p.StartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(p => p.EndDate)
            .IsRequired(false)
            .HasColumnType("date");

        builder.Property(p => p.AssignedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        // Composite index for querying employee assignments by project (non-unique to preserve historical records)
        builder.HasIndex(p => new { p.EmployeeId, p.ProjectId })
            .HasDatabaseName("IX_ProjectAssignmentReferences_EmployeeId_ProjectId");

        // Index for rapid date-range attendance validation
        builder.HasIndex(p => new { p.EmployeeId, p.IsActive, p.StartDate, p.EndDate })
            .HasDatabaseName("IX_ProjectAssignmentReferences_EmployeeId_Dates");
    }
}
