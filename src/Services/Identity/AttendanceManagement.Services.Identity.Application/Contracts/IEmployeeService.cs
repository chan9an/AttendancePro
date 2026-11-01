using AttendanceManagement.Services.Identity.Application.Common.Models;
using AttendanceManagement.Services.Identity.Application.DTOs.Employee;
using AttendanceManagement.Services.Identity.Domain.Enums;

namespace AttendanceManagement.Services.Identity.Application.Contracts;

/// <summary>
/// Service contract for enterprise employee master data and hierarchy operations.
/// </summary>
public interface IEmployeeService
{
    Task<EmployeeResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeResponseDto> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<PagedResult<EmployeeResponseDto>> GetAllAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto request, CancellationToken cancellationToken = default);
    Task<EmployeeResponseDto> UpdateAsync(Guid id, UpdateEmployeeDto request, CancellationToken cancellationToken = default);
    Task AssignGradeAsync(Guid id, EmployeeGrade newGrade, CancellationToken cancellationToken = default);
    Task AssignReportingManagerAsync(Guid id, Guid? managerId, CancellationToken cancellationToken = default);
    Task DeactivateEmployeeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReportingHierarchyDto> GetReportingHierarchyAsync(Guid managerId, CancellationToken cancellationToken = default);
}
