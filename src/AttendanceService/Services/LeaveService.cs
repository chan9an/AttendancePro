using AttendanceService.Clients;
using AttendanceService.Common;
using AttendanceService.DTOs;
using AttendanceService.Models;
using AttendanceService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AttendanceService.Services;

/// <summary>
/// Implements leave applications, approval workflows, and append-only transaction ledger balance tracking.
/// Handles optimistic concurrency and employee-level database locking (sp_getapplock) during review and cancellation
/// to physically prevent negative balances and duplicate ledger entries from concurrent manager reviews.
/// </summary>
public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IIdentityIntegrationService _identityService;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        ILeaveRepository leaveRepository,
        IAttendanceRepository attendanceRepository,
        IIdentityIntegrationService identityService,
        ILogger<LeaveService> logger)
    {
        _leaveRepository = leaveRepository;
        _attendanceRepository = attendanceRepository;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<LeaveRequestResponseDto> ApplyLeaveAsync(ApplyLeaveDto request, CancellationToken cancellationToken = default)
    {
        // 0. Identity Integration: Confirm employee existence and active employment status
        bool isEmployeeActive = await _identityService.IsEmployeeActiveAsync(request.EmployeeId, cancellationToken);
        if (!isEmployeeActive)
        {
            _logger.LogWarning("Leave application rejected: Employee {EmployeeId} does not exist or is not active in Identity Service.", request.EmployeeId);
            throw new DomainException($"Employee {request.EmployeeId} does not exist or is not active. Leave application rejected.");
        }

        var leaveType = (LeaveType)request.LeaveType;

        // 1. Balance verification for paid leave types against the append-only ledger
        if (leaveType != LeaveType.Unpaid)
        {
            decimal availableBalance = await _leaveRepository.GetAvailableBalanceAsync(request.EmployeeId, cancellationToken);
            decimal pendingDays = await _leaveRepository.GetPendingLeaveDaysAsync(request.EmployeeId, excludeLeaveRequestId: null, cancellationToken);

            if (availableBalance - pendingDays < request.TotalDays)
            {
                throw new DomainException(
                    $"Insufficient leave balance. Available: {availableBalance:F1} days, Pending Approval: {pendingDays:F1} days, Requested: {request.TotalDays:F1} days.");
            }
        }

        // 2. Prevent overlapping leaves
        bool overlapExists = await _leaveRepository.HasOverlappingLeaveAsync(
            request.EmployeeId,
            request.StartDate,
            request.EndDate,
            excludeLeaveRequestId: null,
            cancellationToken);

        if (overlapExists)
        {
            throw new ConflictException("A leave request already exists covering part or all of the requested date range.");
        }

        // 3. Two-way conflict check: cannot apply for leave on dates where attendance was already logged and approved
        bool hasApprovedAttendance = await _attendanceRepository.HasApprovedAttendanceInRangeAsync(
            request.EmployeeId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        if (hasApprovedAttendance)
        {
            throw new ConflictException("Cannot apply for leave on dates where attendance has already been recorded and approved.");
        }

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            LeaveType = leaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = request.TotalDays,
            Reason = request.Reason.Trim(),
            Status = LeaveStatus.Submitted,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _leaveRepository.AddLeaveRequestAsync(leaveRequest, cancellationToken);
        await _leaveRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Submitted LeaveRequest {LeaveRequestId} for Employee {EmployeeId} ({TotalDays} days)",
            leaveRequest.Id, leaveRequest.EmployeeId, leaveRequest.TotalDays);

        return MapToDto(leaveRequest);
    }

    public async Task<LeaveRequestResponseDto> UpdateLeaveRequestAsync(Guid id, UpdateLeaveRequestDto request, CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetLeaveRequestByIdAsync(id, cancellationToken);
        if (leave is null)
        {
            throw new NotFoundException(nameof(LeaveRequest), id);
        }

        // 1. Overlap verification excluding the current request being updated
        bool overlapExists = await _leaveRepository.HasOverlappingLeaveAsync(
            leave.EmployeeId,
            request.StartDate,
            request.EndDate,
            excludeLeaveRequestId: id,
            cancellationToken);

        if (overlapExists)
        {
            throw new ConflictException("A leave request already exists covering part or all of the updated date range.");
        }

        // 2. Prevent updating leave to dates where attendance was already logged and approved
        bool hasApprovedAttendance = await _attendanceRepository.HasApprovedAttendanceInRangeAsync(
            leave.EmployeeId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        if (hasApprovedAttendance)
        {
            throw new ConflictException("Cannot update leave to dates where attendance has already been recorded and approved.");
        }

        // 3. Balance verification for paid leaves against append-only ledger
        if (leave.LeaveType != LeaveType.Unpaid)
        {
            decimal availableBalance = await _leaveRepository.GetAvailableBalanceAsync(leave.EmployeeId, cancellationToken);
            decimal otherPendingDays = await _leaveRepository.GetPendingLeaveDaysAsync(leave.EmployeeId, excludeLeaveRequestId: id, cancellationToken);

            if (availableBalance - otherPendingDays < request.TotalDays)
            {
                throw new DomainException(
                    $"Insufficient leave balance for update. Available: {availableBalance:F1} days, Other Pending: {otherPendingDays:F1} days, Requested: {request.TotalDays:F1} days.");
            }
        }

        leave.Update(request.StartDate, request.EndDate, request.TotalDays, request.Reason);

        try
        {
            await _leaveRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating LeaveRequest {LeaveRequestId}", id);
            throw new ConflictException("The leave request was modified by another operation. Please reload and try again.");
        }

        _logger.LogInformation("Updated LeaveRequest {LeaveRequestId}", id);
        return MapToDto(leave);
    }

    public async Task CancelLeaveRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _leaveRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            var leave = await _leaveRepository.GetLeaveRequestByIdAsync(id, cancellationToken);
            if (leave is null)
            {
                throw new NotFoundException(nameof(LeaveRequest), id);
            }

            bool wasApproved = leave.Status == LeaveStatus.Approved;
            leave.Cancel();

            // Transactional Refund: If the cancelled leave was previously approved, issue an immediate refund to the append-only ledger!
            if (wasApproved && leave.LeaveType != LeaveType.Unpaid)
            {
                var now = DateTime.UtcNow;
                var refundEntry = LeaveLedgerEntry.CreateCancellationRefund(
                    employeeId: leave.EmployeeId,
                    leaveRequestId: leave.Id,
                    days: leave.TotalDays,
                    year: now.Year,
                    month: now.Month
                );

                await _leaveRepository.AddLedgerEntryAsync(refundEntry, cancellationToken);
                _logger.LogInformation("Prepared refund of {Days} days for cancelled LeaveRequest {LeaveRequestId}", leave.TotalDays, leave.Id);
            }

            // Atomically commits status change and ledger refund in a single database transaction
            await _leaveRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Cancelled LeaveRequest {LeaveRequestId}", id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Concurrency conflict cancelling LeaveRequest {LeaveRequestId}", id);
            throw new ConflictException("The leave request was modified by another operation. Please reload and try again.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<LeaveRequestResponseDto> ReviewLeaveRequestAsync(Guid id, Guid approverEmployeeId, ReviewLeaveDto review, CancellationToken cancellationToken = default)
    {
        // Database Transaction & Concurrency Strategy:
        // Protects against two different leave requests for the same employee being approved concurrently
        // when their combined total days would exceed the available balance and cause an invalid negative balance.
        await using var transaction = await _leaveRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            var leave = await _leaveRepository.GetLeaveRequestByIdAsync(id, cancellationToken);
            if (leave is null)
            {
                throw new NotFoundException(nameof(LeaveRequest), id);
            }

            if (review.IsApproved)
            {
                // Concurrency Protection: If this is a paid leave, acquire an exclusive transaction-scoped lock
                // on the employee's balance to serialize concurrent approval reviews for the same employee.
                if (leave.LeaveType != LeaveType.Unpaid)
                {
                    await _leaveRepository.AcquireEmployeeBalanceLockAsync(leave.EmployeeId, cancellationToken);

                    // Re-calculate the available balance under the exclusive employee balance lock
                    decimal currentBalance = await _leaveRepository.GetAvailableBalanceAsync(leave.EmployeeId, cancellationToken);

                    if (currentBalance < leave.TotalDays)
                    {
                        throw new DomainException(
                            $"Cannot approve leave request. Available balance ({currentBalance:F1} days) is insufficient for requested {leave.TotalDays:F1} days.");
                    }

                    var now = DateTime.UtcNow;
                    var deductionEntry = LeaveLedgerEntry.CreateLeaveDeduction(
                        employeeId: leave.EmployeeId,
                        leaveRequestId: leave.Id,
                        days: leave.TotalDays,
                        year: now.Year,
                        month: now.Month,
                        leaveType: leave.LeaveType
                    );

                    await _leaveRepository.AddLedgerEntryAsync(deductionEntry, cancellationToken);
                    _logger.LogInformation("Prepared LeaveDeduction of {Days} days for Employee {EmployeeId} on LeaveRequest {LeaveRequestId}",
                        leave.TotalDays, leave.EmployeeId, leave.Id);
                }

                leave.Approve(approverEmployeeId, review.Remarks);
            }
            else
            {
                leave.Reject(approverEmployeeId, review.Remarks ?? "Rejected by manager.");
                _logger.LogInformation("Rejected LeaveRequest {LeaveRequestId} by Approver {ApproverId}", leave.Id, approverEmployeeId);
            }

            // Atomically commits status change and ledger deduction in a single transaction
            await _leaveRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapToDto(leave);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Concurrency conflict during review of LeaveRequest {LeaveRequestId}", id);
            throw new ConflictException("The leave request was reviewed or modified by another operation concurrently. Please reload and try again.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<LeaveRequestResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetLeaveRequestByIdAsync(id, cancellationToken);
        if (leave is null)
        {
            throw new NotFoundException(nameof(LeaveRequest), id);
        }

        return MapToDto(leave);
    }

    public async Task<PagedResult<LeaveRequestResponseDto>> GetByEmployeeAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        int totalCount = await _leaveRepository.GetLeaveRequestsCountByEmployeeAsync(employeeId, cancellationToken);
        var items = await _leaveRepository.GetLeaveRequestsByEmployeePagedAsync(employeeId, pageIndex, pageSize, cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<LeaveRequestResponseDto>(dtos, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<LeaveRequestResponseDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _leaveRepository.GetPendingLeaveRequestsAsync(cancellationToken);
        return items.Select(MapToDto).ToList();
    }

    public async Task<LeaveBalanceResponseDto> GetLeaveBalanceAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        int currentYear = DateTime.UtcNow.Year;

        var transactions = await _leaveRepository.GetLedgerTransactionsByEmployeeAsync(employeeId, cancellationToken);

        decimal availableBalance = transactions.Sum(t => t.Days);

        decimal totalCredited = transactions
            .Where(t => t.AccountingYear == currentYear && t.Days > 0)
            .Sum(t => t.Days);

        decimal totalConsumed = Math.Abs(transactions
            .Where(t => t.AccountingYear == currentYear && t.Days < 0)
            .Sum(t => t.Days));

        var recentDtoList = transactions.Take(10).Select(t => new LeaveLedgerEntryDto(
            Id: t.Id,
            TransactionType: t.TransactionType.ToString(),
            Days: t.Days,
            TransactionDateUtc: t.TransactionDateUtc,
            Description: t.Description,
            AccountingYear: t.AccountingYear,
            AccountingMonth: t.AccountingMonth
        )).ToList();

        return new LeaveBalanceResponseDto(
            EmployeeId: employeeId,
            AvailableBalanceDays: availableBalance,
            TotalCreditedThisYear: totalCredited,
            TotalConsumedThisYear: totalConsumed,
            RecentTransactions: recentDtoList
        );
    }

    private static LeaveRequestResponseDto MapToDto(LeaveRequest leave)
    {
        return new LeaveRequestResponseDto(
            Id: leave.Id,
            EmployeeId: leave.EmployeeId,
            LeaveType: leave.LeaveType.ToString(),
            StartDate: leave.StartDate,
            EndDate: leave.EndDate,
            TotalDays: leave.TotalDays,
            Reason: leave.Reason,
            Status: leave.Status.ToString(),
            ApproverEmployeeId: leave.ApproverEmployeeId,
            ReviewRemarks: leave.ReviewRemarks,
            CreatedAtUtc: leave.CreatedAtUtc
        );
    }
}
