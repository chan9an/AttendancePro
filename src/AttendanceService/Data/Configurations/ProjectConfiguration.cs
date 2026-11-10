using AttendanceService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceService.Data.Configurations;

/// <summary>
/// Fluent API configuration for Project entity.
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Code)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(p => p.Description)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnType("datetime2");

        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_Projects_Name");
    }
}
