using AttendanceManagement.Services.Identity.Common;
using AttendanceManagement.Services.Identity.Data;
using AttendanceManagement.Services.Identity.DTOs;
using AttendanceManagement.Services.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagement.Services.Identity.Services;

/// <summary>
/// Handles employee profile management, grade updates, and manager hierarchies.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IdentityDbContext context, ILogger<EmployeeService> logger)
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
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        return MapToDto(employee);
    }

    public async Task<EmployeeResponseDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException($"Employee profile for UserId '{userId}' was not found.");
        }

        return MapToDto(employee);
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

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<EmployeeResponseDto>(dtos, totalCount, pageIndex, pageSize);
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
        {
            throw new ConflictException("An employee with this corporate email already exists.");
        }

        if (request.ReportingManagerId.HasValue)
        {
            bool managerExists = await _context.Employees
                .AnyAsync(e => e.Id == request.ReportingManagerId.Value && e.Status == EmploymentStatus.Active, cancellationToken);

            if (!managerExists)
            {
                throw new NotFoundException("Designated reporting manager not found or is inactive.");
            }
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Grade = (EmployeeGrade)request.Grade,
            Department = (DepartmentType)request.Department,
            Designation = request.Designation.Trim(),
            JoiningDate = request.JoiningDate,
            ReportingManagerId = request.ReportingManagerId,
            Status = EmploymentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new Employee {EmployeeId} for User {UserId}", employee.Id, employee.UserId);
        return MapToDto(employee);
    }

    public async Task<EmployeeResponseDto> UpdateAsync(Guid id, UpdateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        employee.UpdateProfile(
            request.FirstName,
            request.LastName,
            (DepartmentType)request.Department,
            request.Designation);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated Employee profile for {EmployeeId}", id);

        return MapToDto(employee);
    }

    public async Task AssignGradeAsync(Guid id, EmployeeGrade newGrade, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        employee.AssignGrade(newGrade);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned Grade {Grade} to Employee {EmployeeId}", newGrade, id);
    }

    public async Task AssignReportingManagerAsync(Guid id, Guid? managerId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        if (managerId.HasValue)
        {
            bool managerExists = await _context.Employees
                .AnyAsync(e => e.Id == managerId.Value && e.Status == EmploymentStatus.Active, cancellationToken);

            if (!managerExists)
            {
                throw new NotFoundException("Designated reporting manager does not exist or is inactive.");
            }
        }

        employee.AssignReportingManager(managerId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned Reporting Manager {ManagerId} to Employee {EmployeeId}", managerId, id);
    }

    public async Task DeactivateEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        employee.Deactivate();
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
        {
            throw new NotFoundException(nameof(Employee), managerId);
        }

        var subordinates = manager.Subordinates
            .Where(s => s.Status == EmploymentStatus.Active)
            .Select(MapToDto)
            .ToList();

        return new ReportingHierarchyDto(
            ManagerId: manager.Id,
            ManagerName: manager.FullName,
            ManagerDesignation: manager.Designation,
            Subordinates: subordinates);
    }

    private static EmployeeResponseDto MapToDto(Employee employee)
    {
        string? managerName = employee.ReportingManager != null
            ? employee.ReportingManager.FullName
            : null;

        return new EmployeeResponseDto(
            Id: employee.Id,
            UserId: employee.UserId,
            FirstName: employee.FirstName,
            LastName: employee.LastName,
            FullName: employee.FullName,
            Email: employee.Email,
            Grade: employee.Grade.ToString(),
            Department: employee.Department.ToString(),
            Designation: employee.Designation,
            JoiningDate: employee.JoiningDate,
            ReportingManagerId: employee.ReportingManagerId,
            ReportingManagerName: managerName,
            Status: employee.Status.ToString(),
            CreatedAtUtc: employee.CreatedAtUtc
        );
    }
}
