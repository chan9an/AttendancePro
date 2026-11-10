using AttendanceService.Clients;
using AttendanceService.Common;
using AttendanceService.Models;
using AttendanceService.Repositories;

namespace AttendanceService.Services;

/// <summary>
/// Implements monthly grade-based leave accrual processing and manual ledger adjustments.
/// Records credits directly to the append-only transaction ledger.
/// Enforces database transactions and database-level unique constraints (UX_LeaveLedgerEntries_MonthlyCredit_Idempotency)
/// to guarantee that partial failures roll back completely and monthly credits cannot be duplicated.
/// Note: Default Grade A, B, and C entitlement accrual rates and annual caps are provisional sample values
/// configured in GradeLeaveEntitlements table and are subject to official business confirmation.
/// </summary>
public class LeaveCreditService : ILeaveCreditService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IIdentityIntegrationService _identityService;
    private readonly ILogger<LeaveCreditService> _logger;

    public LeaveCreditService(
        ILeaveRepository leaveRepository,
        IProjectRepository projectRepository,
        IAttendanceRepository attendanceRepository,
        IIdentityIntegrationService identityService,
        ILogger<LeaveCreditService> logger)
    {
        _leaveRepository = leaveRepository;
        _projectRepository = projectRepository;
        _attendanceRepository = attendanceRepository;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<int> ProcessMonthlyCreditsAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating Monthly Leave Credit Processing for Period: {Year}-{Month:D2}", year, month);

        // Explicit database transaction: Ensures all-or-nothing execution.
        // Partial processing failures roll back completely to prevent inconsistent credit records.
        await using var transaction = await _leaveRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Fetch configured grade policies from database.
            // If table is unseeded, populate provisional sample values (configurable through the database).
            var gradePolicies = await _leaveRepository.GetGradeLeaveEntitlementsAsync(cancellationToken);
            if (!gradePolicies.Any())
            {
                _logger.LogWarning("No GradeLeaveEntitlements found. Seeding provisional sample grade entitlement policies.");
                var defaults = new List<GradeLeaveEntitlement>
                {
                    GradeLeaveEntitlement.Create(1, "GradeC", 1.50m, 18.00m), // Provisional sample rate
                    GradeLeaveEntitlement.Create(2, "GradeB", 2.00m, 24.00m), // Provisional sample rate
                    GradeLeaveEntitlement.Create(3, "GradeA", 2.50m, 30.00m)  // Provisional sample rate
                };
                await _leaveRepository.AddGradeEntitlementsAsync(defaults, cancellationToken);
                await _leaveRepository.SaveChangesAsync(cancellationToken);
                gradePolicies = defaults;
            }

            var policyByGrade = gradePolicies.ToDictionary(g => g.Grade);

            // 2. Identify all active employees in this service scope
            var assignedEmployeeIds = await _projectRepository.GetActiveEmployeeIdsAsync(cancellationToken);
            var attendanceEmployeeIds = await _attendanceRepository.GetAllEmployeeIdsAsync(cancellationToken);
            var employeeIds = assignedEmployeeIds.Union(attendanceEmployeeIds).Distinct().ToList();

            int processedCount = 0;

            foreach (var empId in employeeIds)
            {
                // 3. Idempotency Check: Verify if credit was already applied for this employee in target Year & Month
                bool alreadyCredited = await _leaveRepository.HasMonthlyCreditAsync(empId, year, month, cancellationToken);

                if (alreadyCredited)
                {
                    _logger.LogDebug("Monthly credit for Employee {EmployeeId} ({Year}-{Month}) already exists. Skipping.", empId, year, month);
                    continue;
                }

                // 4. Resolve employee's configured grade dynamically from Identity Service HTTP endpoint
                int gradeNumeric = 2; // Default fallback to Grade B (Grade 2)
                var profile = await _identityService.GetEmployeeProfileAsync(empId, cancellationToken);
                if (profile != null)
                {
                    gradeNumeric = profile.Grade switch
                    {
                        "GradeA" => 3,
                        "GradeB" => 2,
                        "GradeC" => 1,
                        _ => 2
                    };
                }

                var policy = policyByGrade.GetValueOrDefault(gradeNumeric, policyByGrade.GetValueOrDefault(2, policyByGrade.Values.First()));

                // 5. Enforce annual maximum cap rule against the append-only ledger
                decimal currentYearCredits = await _leaveRepository.GetTotalCreditedDaysInYearAsync(empId, year, cancellationToken);

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

                await _leaveRepository.AddLedgerEntryAsync(ledgerEntry, cancellationToken);
                processedCount++;
            }

            if (processedCount > 0)
            {
                await _leaveRepository.SaveChangesAsync(cancellationToken);
            }

            // Commit atomic transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully processed monthly leave credits for {Count} employees for {Year}-{Month:D2}", processedCount, year, month);
            return processedCount;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process monthly leave credits for {Year}-{Month:D2}. Transaction rolled back.", year, month);
            throw;
        }
    }

    public async Task RecordAdjustmentAsync(Guid employeeId, decimal days, string reason, CancellationToken cancellationToken = default)
    {
        // Enforce employee-level balance locking and check constraint if negative adjustment could push balance negative
        await using var transaction = await _leaveRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            if (days < 0)
            {
                // Lock employee balance to prevent race conditions with concurrent leave approvals
                await _leaveRepository.AcquireEmployeeBalanceLockAsync(employeeId, cancellationToken);

                decimal currentBalance = await _leaveRepository.GetAvailableBalanceAsync(employeeId, cancellationToken);
                if (currentBalance + days < 0)
                {
                    throw new DomainException(
                        $"Cannot record negative adjustment of {days:F1} days. Available balance is {currentBalance:F1} days and negative balance is prohibited.");
                }
            }

            var now = DateTime.UtcNow;
            var entry = LeaveLedgerEntry.CreateAdjustment(
                employeeId: employeeId,
                days: days,
                year: now.Year,
                month: now.Month,
                reason: reason
            );

            await _leaveRepository.AddLedgerEntryAsync(entry, cancellationToken);
            await _leaveRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Recorded administrative ledger adjustment for Employee {EmployeeId}: {Days} days. Reason: {Reason}", employeeId, days, reason);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
