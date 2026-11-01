using AttendanceManagement.Services.Identity.Application.DTOs.Employee;
using AttendanceManagement.Services.Identity.Domain.Entities;

namespace AttendanceManagement.Services.Identity.Application.Mappings;

/// <summary>
/// Static extension methods providing compile-time safe, reflection-free object mapping.
/// Outperforms AutoMapper by avoiding runtime reflection overhead and expression tree compilation.
/// </summary>
public static class EmployeeMappingExtensions
{
    public static EmployeeResponseDto ToResponseDto(this Employee employee)
    {
        string? managerName = employee.ReportingManager != null
            ? $"{employee.ReportingManager.FirstName} {employee.ReportingManager.LastName}".Trim()
            : null;

        return new EmployeeResponseDto(
            Id: employee.Id,
            UserId: employee.UserId,
            FirstName: employee.FirstName,
            LastName: employee.LastName,
            FullName: $"{employee.FirstName} {employee.LastName}".Trim(),
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

    public static IReadOnlyList<EmployeeResponseDto> ToResponseDtoList(this IEnumerable<Employee> employees)
    {
        return employees.Select(ToResponseDto).ToList();
    }
}
