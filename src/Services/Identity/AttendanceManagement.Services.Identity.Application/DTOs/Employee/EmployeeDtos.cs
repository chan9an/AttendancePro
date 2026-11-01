namespace AttendanceManagement.Services.Identity.Application.DTOs.Employee;

/// <summary>
/// Payload to create an employee profile.
/// </summary>
public record CreateEmployeeDto(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    int Grade,
    int Department,
    string Designation,
    DateTime JoiningDate,
    Guid? ReportingManagerId);

/// <summary>
/// Payload to update employee personal and department details.
/// </summary>
public record UpdateEmployeeDto(
    string FirstName,
    string LastName,
    int Department,
    string Designation);

/// <summary>
/// Response projection representing an employee profile.
/// </summary>
public record EmployeeResponseDto(
    Guid Id,
    string UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Grade,
    string Department,
    string Designation,
    DateTime JoiningDate,
    Guid? ReportingManagerId,
    string? ReportingManagerName,
    string Status,
    DateTime CreatedAtUtc);

/// <summary>
/// Hierarchical view showing a manager and all direct reports.
/// </summary>
public record ReportingHierarchyDto(
    Guid ManagerId,
    string ManagerName,
    string ManagerDesignation,
    IReadOnlyList<EmployeeResponseDto> Subordinates);

/// <summary>
/// Payload to assign or change an employee's grade.
/// </summary>
public record AssignGradeDto(int NewGrade);

/// <summary>
/// Payload to assign or reassign a reporting manager.
/// </summary>
public record AssignManagerDto(Guid? ManagerId);
