using AttendanceManagement.Services.Attendance.Application.Common.Exceptions;
using AttendanceManagement.Services.Attendance.Application.Common.Interfaces;
using AttendanceManagement.Services.Attendance.Application.Common.Models;
using AttendanceManagement.Services.Attendance.Application.Contracts;
using AttendanceManagement.Services.Attendance.Application.DTOs.Attendance;
using AttendanceManagement.Services.Attendance.Application.Mappings;
using AttendanceManagement.Services.Attendance.Domain.Entities;
using AttendanceManagement.Services.Attendance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceManagement.Services.Attendance.Application.Services;

/// <summary>
/// Implements enterprise attendance workflows and the Attendance Auto-Approval Engine.
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceAppDbContext _context;
    private readonly IIdentityIntegrationService _identityService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IAttendanceAppDbContext context,
        IIdentityIntegrationService identityService,
        ILogger<AttendanceService> logger)
    {
        _context = context;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<AttendanceResponseDto> CreateAttendanceAsync(CreateAttendanceDto request, CancellationToken cancellationToken = default)
    {
        // 1. Business Rule: Attendance must not be recorded on dates with approved leave!
        bool hasApprovedLeave = await _context.LeaveRequests
            .AsNoTracking()
            .AnyAsync(l => l.EmployeeId == request.EmployeeId &&
                           l.Status == LeaveStatus.Approved &&
                           l.StartDate <= request.Date &&
                           l.EndDate >= request.Date,
                      cancellationToken);

        if (hasApprovedLeave)
        {
            _logger.LogWarning("Attendance rejected: Employee {EmployeeId} has approved leave on {Date}", request.EmployeeId, request.Date);
            throw new BusinessRuleViolationException("LeaveConflict", $"Cannot record attendance on {request.Date} because the employee has an approved leave for this date.");
        }

        // 2. Business Rule: Prevent duplicate attendance records on the same date
        bool attendanceExists = await _context.AttendanceRecords
            .AsNoTracking()
            .AnyAsync(a => a.EmployeeId == request.EmployeeId &&
                           a.Date == request.Date &&
                           a.Status != AttendanceStatus.Cancelled,
                      cancellationToken);

        if (attendanceExists)
        {
            throw new BusinessRuleViolationException("DuplicateAttendance", $"An active attendance record already exists for employee {request.EmployeeId} on {request.Date}.");
        }

        // 3. Auto-Approval Evaluation:
        // Attendance is automatically approved when:
        // a. The employee is assigned to the specified project.
        // b. No leave request exists for the same date (already checked above).
        // If not assigned to project, record is flagged as PendingApproval for manager review.
        bool isAssignedToProject = await _context.ProjectAssignmentReferences
            .AsNoTracking()
            .AnyAsync(p => p.EmployeeId == request.EmployeeId &&
                           p.ProjectId == request.ProjectId &&
                           p.IsActive,
                      cancellationToken);

        var attendanceRecord = AttendanceRecord.Create(
            id: Guid.NewGuid(),
            employeeId: request.EmployeeId,
            date: request.Date,
            projectId: request.ProjectId,
            projectName: request.ProjectName,
            remarks: request.Remarks,
            isAssignedToProject: isAssignedToProject
        );

        _context.AttendanceRecords.Add(attendanceRecord);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded attendance {AttendanceId} for Employee {EmployeeId} on {Date} with status {Status}",
            attendanceRecord.Id, attendanceRecord.EmployeeId, attendanceRecord.Date, attendanceRecord.Status);

        return attendanceRecord.ToResponseDto();
    }

    public async Task<AttendanceResponseDto> UpdateAttendanceAsync(Guid id, UpdateAttendanceDto request, CancellationToken cancellationToken = default)
    {
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (record is null)
            throw new NotFoundException(nameof(AttendanceRecord), id);

        bool isAssigned = await _context.ProjectAssignmentReferences
            .AsNoTracking()
            .AnyAsync(p => p.EmployeeId == record.EmployeeId && p.ProjectId == request.ProjectId && p.IsActive, cancellationToken);

        record.Update(request.Remarks, request.ProjectId, request.ProjectName, isAssigned);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated attendance {AttendanceId}", id);
        return record.ToResponseDto();
    }

    public async Task CancelAttendanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (record is null)
            throw new NotFoundException(nameof(AttendanceRecord), id);

        record.Cancel();
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled attendance {AttendanceId}", id);
    }

    public async Task<AttendanceResponseDto> ApproveAttendanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (record is null)
            throw new NotFoundException(nameof(AttendanceRecord), id);

        record.Approve();
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Manually approved attendance {AttendanceId}", id);
        return record.ToResponseDto();
    }

    public async Task<AttendanceResponseDto> RejectAttendanceAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (record is null)
            throw new NotFoundException(nameof(AttendanceRecord), id);

        record.Reject(reason);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Rejected attendance {AttendanceId} with reason: {Reason}", id, reason);
        return record.ToResponseDto();
    }

    public async Task<AttendanceResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _context.AttendanceRecords.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (record is null)
            throw new NotFoundException(nameof(AttendanceRecord), id);

        return record.ToResponseDto();
    }

    public async Task<PagedResult<AttendanceResponseDto>> GetByEmployeeAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.Date);

        int totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<AttendanceResponseDto>(items.ToResponseDtoList(), totalCount, pageIndex, pageSize);
    }

    public async Task<DailyAttendanceReportDto> GetDailyReportAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date == date && a.Status != AttendanceStatus.Cancelled)
            .ToListAsync(cancellationToken);

        int present = records.Count(r => r.Status == AttendanceStatus.Approved);
        int autoApproved = records.Count(r => r.Status == AttendanceStatus.Approved);
        int pending = records.Count(r => r.Status == AttendanceStatus.PendingApproval);

        return new DailyAttendanceReportDto(
            Date: date,
            TotalPresent: present,
            TotalAutoApproved: autoApproved,
            TotalPendingApproval: pending,
            Records: records.ToResponseDtoList()
        );
    }

    public async Task<AttendanceSummaryReportDto> GetWeeklyReportAsync(Guid? employeeId, Guid? projectId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        var weekEndDate = weekStartDate.AddDays(6);

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date >= weekStartDate && a.Date <= weekEndDate && a.Status != AttendanceStatus.Cancelled);

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        var items = await query.OrderBy(a => a.Date).ToListAsync(cancellationToken);

        return new AttendanceSummaryReportDto(
            EmployeeId: employeeId,
            ProjectId: projectId,
            StartDate: weekStartDate,
            EndDate: weekEndDate,
            TotalWorkingDays: 7,
            ApprovedDays: items.Count(i => i.Status == AttendanceStatus.Approved),
            PendingDays: items.Count(i => i.Status == AttendanceStatus.PendingApproval),
            RejectedDays: items.Count(i => i.Status == AttendanceStatus.Rejected),
            Details: items.ToResponseDtoList()
        );
    }

    public async Task<AttendanceSummaryReportDto> GetMonthlyReportAsync(Guid? employeeId, Guid? projectId, int year, int month, CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var endDate = new DateOnly(year, month, daysInMonth);

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date >= startDate && a.Date <= endDate && a.Status != AttendanceStatus.Cancelled);

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        var items = await query.OrderBy(a => a.Date).ToListAsync(cancellationToken);

        return new AttendanceSummaryReportDto(
            EmployeeId: employeeId,
            ProjectId: projectId,
            StartDate: startDate,
            EndDate: endDate,
            TotalWorkingDays: daysInMonth,
            ApprovedDays: items.Count(i => i.Status == AttendanceStatus.Approved),
            PendingDays: items.Count(i => i.Status == AttendanceStatus.PendingApproval),
            RejectedDays: items.Count(i => i.Status == AttendanceStatus.Rejected),
            Details: items.ToResponseDtoList()
        );
    }
}
