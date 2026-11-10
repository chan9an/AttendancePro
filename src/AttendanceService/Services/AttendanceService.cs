using AttendanceService.Clients;
using AttendanceService.Common;
using AttendanceService.DTOs;
using AttendanceService.Models;
using AttendanceService.Repositories;

namespace AttendanceService.Services;

/// <summary>
/// Implements attendance workflows, validation, reporting, and auto-approval logic.
/// Confirms employee existence and active status through Identity Service HTTP endpoints.
/// Business Rule: Attendance submission strictly requires a valid, active project assignment spanning the attendance date.
/// Unassigned submissions are rejected immediately instead of creating PendingApproval records.
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IIdentityIntegrationService _identityService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        ILeaveRepository leaveRepository,
        IProjectRepository projectRepository,
        IIdentityIntegrationService identityService,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _leaveRepository = leaveRepository;
        _projectRepository = projectRepository;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<AttendanceResponseDto> CreateAttendanceAsync(CreateAttendanceDto request, CancellationToken cancellationToken = default)
    {
        // 0. Identity Integration: Confirm employee existence and active employment status via Identity Service HTTP client
        bool isEmployeeActive = await _identityService.IsEmployeeActiveAsync(request.EmployeeId, cancellationToken);
        if (!isEmployeeActive)
        {
            _logger.LogWarning("Attendance rejected: Employee {EmployeeId} does not exist or is not active in Identity Service.", request.EmployeeId);
            throw new DomainException($"Employee {request.EmployeeId} does not exist or is not active. Attendance submission rejected.");
        }

        // 1. Business Rule: Attendance cannot be recorded on dates with approved leave
        bool hasApprovedLeave = await _leaveRepository.HasApprovedLeaveOnDateAsync(
            request.EmployeeId,
            request.Date,
            cancellationToken);

        if (hasApprovedLeave)
        {
            _logger.LogWarning("Attendance rejected: Employee {EmployeeId} has approved leave on {Date}", request.EmployeeId, request.Date);
            throw new ConflictException($"Cannot record attendance on {request.Date} because the employee has an approved leave.");
        }

        // 2. Business Rule: Prevent duplicate active attendance on the same date (Status <> Cancelled)
        bool attendanceExists = await _attendanceRepository.HasActiveAttendanceOnDateAsync(
            request.EmployeeId,
            request.Date,
            cancellationToken);

        if (attendanceExists)
        {
            throw new ConflictException($"An active attendance record already exists for employee {request.EmployeeId} on {request.Date}.");
        }

        // 3. Project Assignment Validation:
        // Check assignment start and end dates against the attendance date, in addition to IsActive.
        // Explicit Business Rule Decision: Require a valid assignment for attendance submission.
        // Reject unassigned project attendance immediately instead of silently creating PendingApproval records.
        bool hasValidAssignment = await _projectRepository.HasValidAssignmentOnDateAsync(
            request.EmployeeId,
            request.ProjectId,
            request.Date,
            cancellationToken);

        if (!hasValidAssignment)
        {
            _logger.LogWarning("Attendance submission rejected: Employee {EmployeeId} does not have an active assignment to project {ProjectId} on {Date}",
                request.EmployeeId, request.ProjectId, request.Date);
            throw new DomainException($"Employee is not actively assigned to project '{request.ProjectName}' on {request.Date}. Attendance submission rejected.");
        }

        // 4. Record attendance as Approved (auto-approved via validated project assignment)
        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            ProjectId = request.ProjectId,
            ProjectName = request.ProjectName.Trim(),
            Remarks = request.Remarks?.Trim(),
            Status = AttendanceStatus.Approved,
            IsAutoApproved = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _attendanceRepository.AddAsync(record, cancellationToken);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded auto-approved attendance {AttendanceId} for Employee {EmployeeId} on {Date} for Project {ProjectName}",
            record.Id, record.EmployeeId, record.Date, record.ProjectName);

        return MapToDto(record);
    }

    public async Task<AttendanceResponseDto> UpdateAttendanceAsync(Guid id, UpdateAttendanceDto request, CancellationToken cancellationToken = default)
    {
        var record = await _attendanceRepository.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            throw new NotFoundException(nameof(AttendanceRecord), id);
        }

        // Validate that employee has a valid assignment for the new project on the attendance date
        bool hasValidAssignment = await _projectRepository.HasValidAssignmentOnDateAsync(
            record.EmployeeId,
            request.ProjectId,
            record.Date,
            cancellationToken);

        if (!hasValidAssignment)
        {
            _logger.LogWarning("Attendance update rejected: Employee {EmployeeId} has no active assignment to project {ProjectId} on date {Date}",
                record.EmployeeId, request.ProjectId, record.Date);
            throw new DomainException($"Employee is not actively assigned to project '{request.ProjectName}' on {record.Date}. Attendance update rejected.");
        }

        record.Update(request.Remarks, request.ProjectId, request.ProjectName, isAssignedToProject: true);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated attendance {AttendanceId}", id);
        return MapToDto(record);
    }

    public async Task CancelAttendanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _attendanceRepository.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            throw new NotFoundException(nameof(AttendanceRecord), id);
        }

        record.Cancel();
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled attendance {AttendanceId}", id);
    }

    public async Task<AttendanceResponseDto> ApproveAttendanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _attendanceRepository.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            throw new NotFoundException(nameof(AttendanceRecord), id);
        }

        record.Approve();
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Approved attendance {AttendanceId}", id);
        return MapToDto(record);
    }

    public async Task<AttendanceResponseDto> RejectAttendanceAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var record = await _attendanceRepository.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            throw new NotFoundException(nameof(AttendanceRecord), id);
        }

        record.Reject(reason);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Rejected attendance {AttendanceId} with reason: {Reason}", id, reason);
        return MapToDto(record);
    }

    public async Task<AttendanceResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _attendanceRepository.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            throw new NotFoundException(nameof(AttendanceRecord), id);
        }

        return MapToDto(record);
    }

    public async Task<PagedResult<AttendanceResponseDto>> GetByEmployeeAsync(Guid employeeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        int totalCount = await _attendanceRepository.GetCountByEmployeeAsync(employeeId, cancellationToken);
        var items = await _attendanceRepository.GetByEmployeePagedAsync(employeeId, pageIndex, pageSize, cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<AttendanceResponseDto>(dtos, totalCount, pageIndex, pageSize);
    }

    public async Task<DailyAttendanceReportDto> GetDailyReportAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var records = await _attendanceRepository.GetByDateAsync(date, cancellationToken);

        int present = records.Count(r => r.Status == AttendanceStatus.Approved);
        int autoApproved = records.Count(r => r.Status == AttendanceStatus.Approved && r.IsAutoApproved);
        int pending = records.Count(r => r.Status == AttendanceStatus.PendingApproval);

        return new DailyAttendanceReportDto(
            Date: date,
            TotalPresent: present,
            TotalAutoApproved: autoApproved,
            TotalPendingApproval: pending,
            Records: records.Select(MapToDto).ToList()
        );
    }

    public async Task<AttendanceSummaryReportDto> GetWeeklyReportAsync(Guid? employeeId, Guid? projectId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        var weekEndDate = weekStartDate.AddDays(6);
        var items = await _attendanceRepository.GetByDateRangeAsync(weekStartDate, weekEndDate, employeeId, projectId, cancellationToken);

        return new AttendanceSummaryReportDto(
            EmployeeId: employeeId,
            ProjectId: projectId,
            StartDate: weekStartDate,
            EndDate: weekEndDate,
            TotalWorkingDays: 7,
            ApprovedDays: items.Count(i => i.Status == AttendanceStatus.Approved),
            PendingDays: items.Count(i => i.Status == AttendanceStatus.PendingApproval),
            RejectedDays: items.Count(i => i.Status == AttendanceStatus.Rejected),
            Details: items.Select(MapToDto).ToList()
        );
    }

    public async Task<AttendanceSummaryReportDto> GetMonthlyReportAsync(Guid? employeeId, Guid? projectId, int year, int month, CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var endDate = new DateOnly(year, month, daysInMonth);

        var items = await _attendanceRepository.GetByDateRangeAsync(startDate, endDate, employeeId, projectId, cancellationToken);

        return new AttendanceSummaryReportDto(
            EmployeeId: employeeId,
            ProjectId: projectId,
            StartDate: startDate,
            EndDate: endDate,
            TotalWorkingDays: daysInMonth,
            ApprovedDays: items.Count(i => i.Status == AttendanceStatus.Approved),
            PendingDays: items.Count(i => i.Status == AttendanceStatus.PendingApproval),
            RejectedDays: items.Count(i => i.Status == AttendanceStatus.Rejected),
            Details: items.Select(MapToDto).ToList()
        );
    }

    private static AttendanceResponseDto MapToDto(AttendanceRecord record)
    {
        return new AttendanceResponseDto(
            Id: record.Id,
            EmployeeId: record.EmployeeId,
            Date: record.Date,
            ProjectId: record.ProjectId,
            ProjectName: record.ProjectName,
            Remarks: record.Remarks,
            Status: record.Status.ToString(),
            CreatedAtUtc: record.CreatedAtUtc
        );
    }
}
