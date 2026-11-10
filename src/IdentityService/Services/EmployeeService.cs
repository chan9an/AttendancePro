using IdentityService.Common;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

/// <summary>
/// Handles employee profile management, grade updates, and manager hierarchies.
/// Uses IEmployeeRepository for clean data-access separation.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IEmployeeRepository employeeRepository, ILogger<EmployeeService> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<EmployeeResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, asNoTracking: true, cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        return MapToDto(employee);
    }

    public async Task<EmployeeResponseDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);

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

        var (items, totalCount) = await _employeeRepository.GetAllPagedAsync(pageIndex, pageSize, cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<EmployeeResponseDto>(dtos, totalCount, pageIndex, pageSize);
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        bool emailExists = await _employeeRepository.EmailExistsAsync(request.Email, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException("An employee with this corporate email already exists.");
        }

        if (request.ReportingManagerId.HasValue)
        {
            bool managerExists = await _employeeRepository.ActiveManagerExistsAsync(request.ReportingManagerId.Value, cancellationToken);

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

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new Employee {EmployeeId} for User {UserId}", employee.Id, employee.UserId);
        return MapToDto(employee);
    }

    public async Task<EmployeeResponseDto> UpdateAsync(Guid id, UpdateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, asNoTracking: false, cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        employee.UpdateProfile(
            request.FirstName,
            request.LastName,
            (DepartmentType)request.Department,
            request.Designation);

        await _employeeRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated Employee profile for {EmployeeId}", id);

        return MapToDto(employee);
    }

    public async Task AssignGradeAsync(Guid id, EmployeeGrade newGrade, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, asNoTracking: false, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        employee.AssignGrade(newGrade);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned Grade {Grade} to Employee {EmployeeId}", newGrade, id);
    }

    public async Task AssignReportingManagerAsync(Guid id, Guid? managerId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, asNoTracking: false, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        if (managerId.HasValue)
        {
            bool managerExists = await _employeeRepository.ActiveManagerExistsAsync(managerId.Value, cancellationToken);

            if (!managerExists)
            {
                throw new NotFoundException("Designated reporting manager does not exist or is inactive.");
            }
        }

        employee.AssignReportingManager(managerId);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned Reporting Manager {ManagerId} to Employee {EmployeeId}", managerId, id);
    }

    public async Task DeactivateEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, asNoTracking: false, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        employee.Deactivate();
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated Employee {EmployeeId}", id);
    }

    public async Task<ReportingHierarchyDto> GetReportingHierarchyAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        var manager = await _employeeRepository.GetWithSubordinatesAsync(managerId, cancellationToken);

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
