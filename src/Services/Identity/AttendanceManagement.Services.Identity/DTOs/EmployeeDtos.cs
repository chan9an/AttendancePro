namespace AttendanceManagement.Services.Identity.DTOs;

/// <summary>
/// Generic container for paginated query results.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageIndex,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

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

public record UpdateEmployeeDto(
    string FirstName,
    string LastName,
    int Department,
    string Designation);

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

public record ReportingHierarchyDto(
    Guid ManagerId,
    string ManagerName,
    string ManagerDesignation,
    IReadOnlyList<EmployeeResponseDto> Subordinates);

public record AssignGradeDto(int NewGrade);

public record AssignManagerDto(Guid? ManagerId);
