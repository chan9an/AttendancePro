using AttendanceManagement.Services.Identity.DTOs;
using AttendanceManagement.Services.Identity.Entities;

namespace AttendanceManagement.Services.Identity.Services;

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
