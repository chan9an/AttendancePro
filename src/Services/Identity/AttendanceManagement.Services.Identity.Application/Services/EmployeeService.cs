using AttendanceManagement.Services.Identity.Application.Common.Exceptions;
using AttendanceManagement.Services.Identity.Application.Common.Interfaces;
using AttendanceManagement.Services.Identity.Application.Common.Models;
using AttendanceManagement.Services.Identity.Application.Contracts;
using AttendanceManagement.Services.Identity.Application.DTOs.Employee;
using AttendanceManagement.Services.Identity.Application.Mappings;
using AttendanceManagement.Services.Identity.Domain.Entities;
using AttendanceManagement.Services.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceManagement.Services.Identity.Application.Services;

/// <summary>
/// Implements business workflows for employee directory and hierarchy management.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IIdentityAppDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IIdentityAppDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmployeeResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
            throw new NotFoundException(nameof(Employee), id);

        return employee.ToResponseDto();
    }

    public async Task<EmployeeResponseDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        if (employee is null)
            throw new NotFoundException($"Employee profile for UserId '{userId}' was not found.");

        return employee.ToResponseDto();
    }

    public async Task<PagedResult<EmployeeResponseDto>> GetAllAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.ReportingManager)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.ToResponseDtoList();
        return new PagedResult<EmployeeResponseDto>(dtos, totalCount, pageIndex, pageSize);
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        // Business Rule: Check for email uniqueness
        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { nameof(request.Email), new[] { "An employee with this email already exists." } }
            });

        // Validate reporting manager if provided
        if (request.ReportingManagerId.HasValue)
        {
            bool managerExists = await _context.Employees
                .AnyAsync(e => e.Id == request.ReportingManagerId.Value && e.Status == EmploymentStatus.Active, cancellationToken);

            if (!managerExists)
                throw new NotFoundException("Designated reporting manager not found or is inactive.");
        }

        var employee = Employee.Create(
            id: Guid.NewGuid(),
            userId: request.UserId,
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            grade: (EmployeeGrade)request.Grade,
            department: (DepartmentType)request.Department,
            designation: request.Designation,
            joiningDate: request.JoiningDate,
            reportingManagerId: request.ReportingManagerId
        );

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new Employee {EmployeeId} for User {UserId}", employee.Id, employee.UserId);

        return employee.ToResponseDto();
    }

    public async Task<EmployeeResponseDto> UpdateAsync(Guid id, UpdateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
            throw new NotFoundException(nameof(Employee), id);

        employee.UpdateProfile(
            request.FirstName,
            request.LastName,
            (DepartmentType)request.Department,
            request.Designation);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated Employee profile for {EmployeeId}", id);

        return employee.ToResponseDto();
    }

    public async Task AssignGradeAsync(Guid id, EmployeeGrade newGrade, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
            throw new NotFoundException(nameof(Employee), id);

        employee.AssignGrade(newGrade);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned new Grade {Grade} to Employee {EmployeeId}", newGrade, id);
    }

    public async Task AssignReportingManagerAsync(Guid id, Guid? managerId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
            throw new NotFoundException(nameof(Employee), id);

        if (managerId.HasValue)
        {
            bool managerExists = await _context.Employees
                .AnyAsync(e => e.Id == managerId.Value && e.Status == EmploymentStatus.Active, cancellationToken);

            if (!managerExists)
                throw new NotFoundException("Designated reporting manager does not exist or is inactive.");
        }

        employee.AssignReportingManager(managerId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned Reporting Manager {ManagerId} to Employee {EmployeeId}", managerId, id);
    }

    public async Task DeactivateEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
            throw new NotFoundException(nameof(Employee), id);

        employee.UpdateStatus(EmploymentStatus.Inactive);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated Employee {EmployeeId}", id);
    }

    public async Task<ReportingHierarchyDto> GetReportingHierarchyAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        var manager = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Subordinates)
            .FirstOrDefaultAsync(e => e.Id == managerId, cancellationToken);

        if (manager is null)
            throw new NotFoundException(nameof(Employee), managerId);

        var subordinatesDto = manager.Subordinates
            .Where(s => s.Status == EmploymentStatus.Active)
            .Select(s => s.ToResponseDto())
            .ToList();

        return new ReportingHierarchyDto(
            ManagerId: manager.Id,
            ManagerName: $"{manager.FirstName} {manager.LastName}".Trim(),
            ManagerDesignation: manager.Designation,
            Subordinates: subordinatesDto);
    }
}
