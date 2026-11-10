using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Data.Configurations;

/// <summary>
/// Fluent API configuration for the Employee entity.
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Designation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Grade)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Department)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.JoiningDate)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(e => e.UpdatedAtUtc)
            .IsRequired(false)
            .HasColumnType("datetime2");

        // Unique corporate email index
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("IX_Employees_Email");

        // Fast lookup index on UserId
        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Employees_UserId");

        // Filtered lookup indexes
        builder.HasIndex(e => e.Grade).HasDatabaseName("IX_Employees_Grade");
        builder.HasIndex(e => e.Department).HasDatabaseName("IX_Employees_Department");

        // Self-referencing reporting manager relationship:
        // One Manager -> Many Subordinates. Restrict delete to prevent cascading accidental deletes.
        builder.HasOne(e => e.ReportingManager)
            .WithMany(m => m.Subordinates)
            .HasForeignKey(e => e.ReportingManagerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
