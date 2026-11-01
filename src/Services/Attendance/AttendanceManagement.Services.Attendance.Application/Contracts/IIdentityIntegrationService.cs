namespace AttendanceManagement.Services.Attendance.Application.Contracts;

/// <summary>
/// Cross-service integration model representing an employee profile retrieved from Identity Service.
/// </summary>
public record EmployeeProfileSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    string Grade,
    string Department,
    string Status);

/// <summary>
/// HTTP Integration contract for retrieving employee profile and status information from Identity & Employee Service.
/// </summary>
public interface IIdentityIntegrationService
{
    Task<EmployeeProfileSummaryDto?> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> IsEmployeeActiveAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
