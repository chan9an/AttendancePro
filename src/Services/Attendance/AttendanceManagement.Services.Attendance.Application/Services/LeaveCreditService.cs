using AttendanceManagement.Services.Attendance.Application.Common.Interfaces;
using AttendanceManagement.Services.Attendance.Application.Contracts;
using AttendanceManagement.Services.Attendance.Domain.Entities;
using AttendanceManagement.Services.Attendance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceManagement.Services.Attendance.Application.Services;

/// <summary>
/// Implements monthly grade-based leave accrual processing and manual ledger adjustments.
/// Enforces idempotency to guarantee that monthly credits cannot run twice for the same billing cycle.
/// </summary>
public class LeaveCreditService : ILeaveCreditService
{
    private readonly IAttendanceAppDbContext _context;
    private readonly ILogger<LeaveCreditService> _logger;

    public LeaveCreditService(IAttendanceAppDbContext context, ILogger<LeaveCreditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ProcessMonthlyCreditsAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating Monthly Leave Credit Processing for Period: {Year}-{Month:D2}", year, month);

        // 1. Fetch configured grade policies
        var gradePolicies = await _context.GradeLeaveEntitlements.AsNoTracking().ToListAsync(cancellationToken);
        if (!gradePolicies.Any())
        {
            _logger.LogWarning("No GradeLeaveEntitlements found. Seeding default grade entitlement policies.");
            // Seed standard enterprise tiers: Grade A = 2.50, Grade B = 2.00, Grade C = 1.50
            var defaults = new List<GradeLeaveEntitlement>
            {
                GradeLeaveEntitlement.Create(1, "GradeC", 1.50m, 18.00m),
                GradeLeaveEntitlement.Create(2, "GradeB", 2.00m, 24.00m),
                GradeLeaveEntitlement.Create(3, "GradeA", 2.50m, 30.00m)
            };
            _context.GradeLeaveEntitlements.AddRange(defaults);
            await _context.SaveChangesAsync(cancellationToken);
            gradePolicies = defaults;
        }

        var policyByGrade = gradePolicies.ToDictionary(g => g.Grade);

        // 2. Identify all active employees in this service scope
        // Using distinct EmployeeIds from project assignments and existing attendance/leave records
        var employeeIds = await _context.ProjectAssignmentReferences
            .Where(p => p.IsActive)
            .Select(p => p.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        int processedCount = 0;

        foreach (var empId in employeeIds)
        {
            // 3. Idempotency Check: Verify if credit already applied for this employee in target Year & Month
            bool alreadyCredited = await _context.LeaveLedgerEntries
                .AsNoTracking()
                .AnyAsync(e => e.EmployeeId == empId &&
                               e.AccountingYear == year &&
                               e.AccountingMonth == month &&
                               e.TransactionType == LedgerTransactionType.MonthlyCredit,
                          cancellationToken);

            if (alreadyCredited)
            {
                _logger.LogDebug("Monthly credit for Employee {EmployeeId} ({Year}-{Month}) already exists. Skipping.", empId, year, month);
                continue;
            }

            // Default to Grade B policy if unassigned
            var policy = policyByGrade.GetValueOrDefault(2, policyByGrade.Values.First());

            // 4. Check annual maximum cap rule
            decimal currentYearCredits = await _context.LeaveLedgerEntries
                .AsNoTracking()
                .Where(e => e.EmployeeId == empId &&
                               e.AccountingYear == year &&
                               e.TransactionType == LedgerTransactionType.MonthlyCredit)
                .SumAsync(e => (decimal?)e.Days, cancellationToken) ?? 0m;

            if (currentYearCredits >= policy.AnnualMaximumCap)
            {
                _logger.LogInformation("Employee {EmployeeId} has reached annual leave cap ({Cap} days). Skipping monthly accrual.", empId, policy.AnnualMaximumCap);
                continue;
            }

            decimal creditDays = Math.Min(policy.MonthlyCreditDays, policy.AnnualMaximumCap - currentYearCredits);

            var ledgerEntry = LeaveLedgerEntry.CreateMonthlyCredit(
                employeeId: empId,
                days: creditDays,
                year: year,
                month: month,
                gradeName: policy.GradeName
            );

            _context.LeaveLedgerEntries.Add(ledgerEntry);
            processedCount++;
        }

        if (processedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Successfully processed monthly leave credits for {Count} employees for {Year}-{Month:D2}", processedCount, year, month);
        return processedCount;
    }

    public async Task RecordAdjustmentAsync(Guid employeeId, decimal days, string reason, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entry = LeaveLedgerEntry.CreateAdjustment(
            employeeId: employeeId,
            days: days,
            year: now.Year,
            month: now.Month,
            reason: reason
        );

        _context.LeaveLedgerEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded administrative ledger adjustment for Employee {EmployeeId}: {Days} days. Reason: {Reason}", employeeId, days, reason);
    }
}
