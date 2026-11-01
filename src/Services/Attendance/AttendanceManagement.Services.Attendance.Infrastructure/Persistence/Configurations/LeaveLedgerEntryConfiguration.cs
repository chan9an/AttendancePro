using AttendanceManagement.Services.Attendance.Domain.Entities;
using AttendanceManagement.Services.Attendance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for LeaveLedgerEntry entity.
/// Defines keys, decimal precision, indexes, and unique idempotency constraints.
/// </summary>
public class LeaveLedgerEntryConfiguration : IEntityTypeConfiguration<LeaveLedgerEntry>
{
    public void Configure(EntityTypeBuilder<LeaveLedgerEntry> builder)
    {
        builder.ToTable("LeaveLedgerEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.LeaveRequestId)
            .IsRequired(false);

        builder.Property(e => e.TransactionType)
            .IsRequired()
            .HasConversion<int>();

        // Precision (5, 2) allows up to 999.99 days with 2 decimal places (e.g., 2.50 or -1.50)
        builder.Property(e => e.Days)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.AccountingYear)
            .IsRequired();

        builder.Property(e => e.AccountingMonth)
            .IsRequired();

        builder.Property(e => e.TransactionDateUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        // General Index on EmployeeId for rapid balance calculation: SUM(Days) WHERE EmployeeId = @Id
        builder.HasIndex(e => e.EmployeeId)
            .HasDatabaseName("IX_LeaveLedgerEntries_EmployeeId");

        // Idempotency Constraint:
        // A unique filtered index ensuring that for a given (EmployeeId, Year, Month),
        // only ONE MonthlyCredit transaction can ever be created!
        // This physically prevents accidental duplicate credit runs at the database engine level.
        builder.HasIndex(e => new { e.EmployeeId, e.AccountingYear, e.AccountingMonth, e.TransactionType })
            .IsUnique()
            .HasFilter("[TransactionType] = 1") // 1 = LedgerTransactionType.MonthlyCredit
            .HasDatabaseName("UX_LeaveLedgerEntries_MonthlyCredit_Idempotency");
    }
}
