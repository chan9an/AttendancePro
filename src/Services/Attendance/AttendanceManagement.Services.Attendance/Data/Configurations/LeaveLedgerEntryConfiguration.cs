using AttendanceManagement.Services.Attendance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Attendance.Data.Configurations;

/// <summary>
/// Fluent API configuration for LeaveLedgerEntry entity.
/// Enforces decimal precision, indexing, monthly credit idempotency, and concurrent approval/refund prevention.
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

        // Index for fast balance summation: SUM(Days) WHERE EmployeeId = @Id
        builder.HasIndex(e => e.EmployeeId)
            .HasDatabaseName("IX_LeaveLedgerEntries_EmployeeId");

        // 1. Idempotency: guarantees an employee can never receive two MonthlyCredit entries for the same year/month
        builder.HasIndex(e => new { e.EmployeeId, e.AccountingYear, e.AccountingMonth, e.TransactionType })
            .IsUnique()
            .HasFilter("[TransactionType] = 1") // 1 = LedgerTransactionType.MonthlyCredit
            .HasDatabaseName("UX_LeaveLedgerEntries_MonthlyCredit_Idempotency");

        // 2. Concurrency Safety: guarantees a leave request cannot receive duplicate deductions from concurrent approvals
        builder.HasIndex(e => new { e.LeaveRequestId, e.TransactionType })
            .IsUnique()
            .HasFilter("[LeaveRequestId] IS NOT NULL AND [TransactionType] = 2") // 2 = LedgerTransactionType.LeaveDeduction
            .HasDatabaseName("UX_LeaveLedgerEntries_LeaveRequestId_Deduction");

        // 3. Concurrency Safety: guarantees a leave request cannot receive duplicate refunds from concurrent cancellations
        builder.HasIndex(e => new { e.LeaveRequestId, e.TransactionType })
            .IsUnique()
            .HasFilter("[LeaveRequestId] IS NOT NULL AND [TransactionType] = 3") // 3 = LedgerTransactionType.CancellationRefund
            .HasDatabaseName("UX_LeaveLedgerEntries_LeaveRequestId_Refund");
    }
}
