using AttendanceManagement.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the Employee entity.
/// Defines keys, indexes, field constraints, and self-referencing relationship rules.
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        // Table Name
        builder.ToTable("Employees");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Properties & Constraints
        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(450); // Matches default AspNetUsers.Id column length

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
            .HasConversion<int>(); // Stores enum value as integer in database

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

        // Indexes
        // 1. Unique index on Email: No two employees can share the same corporate email
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("IX_Employees_Email");

        // 2. Index on UserId for fast lookup during authentication token resolution
        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Employees_UserId");

        // 3. Index on Grade and Department for fast filtered queries
        builder.HasIndex(e => e.Grade)
            .HasDatabaseName("IX_Employees_Grade");

        builder.HasIndex(e => e.Department)
            .HasDatabaseName("IX_Employees_Department");

        // Self-Referencing Relationship: Reporting Manager <-> Subordinates
        // One Manager HasMany Subordinates, Each Subordinate HasOne ReportingManager.
        // DeleteBehavior.Restrict: Prevents accidental cascade deletion of subordinates when a manager is removed!
        builder.HasOne(e => e.ReportingManager)
            .WithMany(m => m.Subordinates)
            .HasForeignKey(e => e.ReportingManagerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
