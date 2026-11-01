using AttendanceManagement.Services.Attendance.Application.Common.Exceptions;
using AttendanceManagement.Services.Attendance.Application.Common.Interfaces;
using AttendanceManagement.Services.Attendance.Application.Common.Models;
using AttendanceManagement.Services.Attendance.Application.Contracts;
using AttendanceManagement.Services.Attendance.Application.DTOs.Leave;
using AttendanceManagement.Services.Attendance.Application.Mappings;
using AttendanceManagement.Services.Attendance.Domain.Entities;
using AttendanceManagement.Services.Attendance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceManagement.Services.Attendance.Application.Services;

/// <summary>
/// Implements enterprise leave application workflows, managerial approvals,
/// and double-entry style transactional leave ledger tracking.
/// </summary>
public class LeaveService : ILeaveService
{
    private readonly IAttendanceAppDbContext _context;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(IAttendanceAppDbContext context, ILogger<LeaveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LeaveRequestResponseDto> ApplyLeaveAsync(ApplyLeaveDto request, CancellationToken cancellationToken = default)
    {
        var leaveType = (LeaveType)request.LeaveType;

        // 1. Balance verification for paid leave types
        if (leaveType != LeaveType.Unpaid)
        {
            decimal availableBalance = await _context.LeaveLedgerEntries
                .AsNoTracking()
                .Where(e => e.EmployeeId == request.EmployeeId)
                .SumAsync(e => (decimal?)e.Days, cancellationToken) ?? 0m;

            // Also account for submitted pending leaves not yet approved/deducted
            decimal pendingDays = await _context.LeaveRequests
                .AsNoTracking()
                .Where(l => l.EmployeeId == request.EmployeeId && l.Status == LeaveStatus.Submitted && l.LeaveType != LeaveType.Unpaid)
                .SumAsync(l => (decimal?)l.TotalDays, cancellationToken) ?? 0m;

            if (availableBalance - pendingDays < request.TotalDays)
            {
                throw new BusinessRuleViolationException(
                    "InsufficientLeaveBalance",
                    $"Insufficient leave balance. Available: {availableBalance:F1} days, Pending Approval: {pendingDays:F1} days, Requested: {request.TotalDays:F1} days.");
            }
        }

        // 2. Prevent overlapping leaves
        bool overlapExists = await _context.LeaveRequests
            .AsNoTracking()
            .AnyAsync(l => l.EmployeeId == request.EmployeeId &&
                           l.Status != LeaveStatus.Rejected &&
                           l.Status != LeaveStatus.Cancelled &&
                           l.StartDate <= request.EndDate &&
                           l.EndDate >= request.StartDate,
                      cancellationToken);

        if (overlapExists)
        {
            throw new BusinessRuleViolationException("OverlappingLeave", "A leave request already exists covering part or all of the requested date range.");
        }

        var leaveRequest = LeaveRequest.Create(
            id: Guid.NewGuid(),
            employeeId: request.EmployeeId,
            leaveType: leaveType,
            startDate: request.StartDate,
            endDate: request.EndDate,
            totalDays: request.TotalDays,
            reason: request.Reason
        );

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Submitted LeaveRequest {LeaveRequestId} for Employee {EmployeeId} ({TotalDays} days)",
            leaveRequest.Id, leaveRequest.EmployeeId, leaveRequest.TotalDays);

        return leaveRequest.ToResponseDto();
    }

    public async Task<LeaveRequestResponseDto> UpdateLeaveRequestAsync(Guid id, UpdateLeaveRequestDto request, CancellationToken cancellationToken = default)
    {
        var leave = await _context.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (leave is null)
            throw new NotFoundException(nameof(LeaveRequest), id);

        leave.Update(request.StartDate, request.EndDate, request.TotalDays, request.Reason);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated LeaveRequest {LeaveRequestId}", id);
        return leave.ToResponseDto();
    }

    public async Task CancelLeaveRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leave = await _context.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (leave is null)
            throw new NotFoundException(nameof(LeaveRequest), id);

        bool wasApproved = leave.Status == LeaveStatus.Approved;
        leave.Cancel();

        // Transactional Consistency:
        // If the cancelled leave was previously approved (and deducted), issue an immediate CancellationRefund to the ledger!
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

            _context.LeaveLedgerEntries.Add(refundEntry);
            _logger.LogInformation("Issued refund of {Days} days to Ledger for cancelled LeaveRequest {LeaveRequestId}", leave.TotalDays, leave.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled LeaveRequest {LeaveRequestId}", id);
    }

    public async Task<LeaveRequestResponseDto> ReviewLeaveRequestAsync(Guid id, Guid approverEmployeeId, ReviewLeaveDto review, CancellationToken cancellationToken = default)
    {
        var leave = await _context.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (leave is null)
            throw new NotFoundException(nameof(LeaveRequest), id);

        if (review.IsApproved)
        {
            leave.Approve(approverEmployeeId, review.Remarks);

            // Double-Entry Ledger: Record deduction transaction upon approval
            if (leave.LeaveType != LeaveType.Unpaid)
            {
                var now = DateTime.UtcNow;
                var deductionEntry = LeaveLedgerEntry.CreateLeaveDeduction(
                    employeeId: leave.EmployeeId,
                    leaveRequestId: leave.Id,
                    days: leave.TotalDays,
                    year: now.Year,
                    month: now.Month,
                    leaveType: leave.LeaveType
                );

                _context.LeaveLedgerEntries.Add(deductionEntry);
                _logger.LogInformation("Recorded LeaveDeduction of {Days} days for Employee {EmployeeId} on LeaveRequest {LeaveRequestId}",
                    leave.TotalDays, leave.EmployeeId, leave.Id);
            }
        }
        else
        {
            leave.Reject(approverEmployeeId, review.Remarks ?? "Rejected by manager.");
            _logger.LogInformation("Rejected LeaveRequest {LeaveRequestId} by Approver {ApproverId}", leave.Id, approverEmployeeId);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return leave.ToResponseDto();
    }

    public async Task<LeaveRequestResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leave = await _context.LeaveRequests.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (leave is null)
            throw new NotFoundException(nameof(LeaveRequest), id);

        return leave.ToResponseDto();
    }

    public async Task<PagedResult<LeaveRequestResponseDto>> GetByEmployeeAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.CreatedAtUtc);

        int totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<LeaveRequestResponseDto>(items.ToResponseDtoList(), totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<LeaveRequestResponseDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Submitted)
            .OrderBy(l => l.StartDate)
            .ToListAsync(cancellationToken);

        return items.ToResponseDtoList();
    }

    public async Task<LeaveBalanceResponseDto> GetLeaveBalanceAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        int currentYear = DateTime.UtcNow.Year;

        var transactions = await _context.LeaveLedgerEntries
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.TransactionDateUtc)
            .ToListAsync(cancellationToken);

        decimal availableBalance = transactions.Sum(t => t.Days);

        decimal totalCredited = transactions
            .Where(t => t.AccountingYear == currentYear && t.Days > 0)
            .Sum(t => t.Days);

        decimal totalConsumed = Math.Abs(transactions
            .Where(t => t.AccountingYear == currentYear && t.Days < 0)
            .Sum(t => t.Days));

        return new LeaveBalanceResponseDto(
            EmployeeId: employeeId,
            AvailableBalanceDays: availableBalance,
            TotalCreditedThisYear: totalCredited,
            TotalConsumedThisYear: totalConsumed,
            RecentTransactions: transactions.Take(10).ToDtoList()
        );
    }
}
