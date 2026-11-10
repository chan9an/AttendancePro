namespace AttendanceService.Clients;

public record EmployeeProfileSummary(
    Guid Id,
    string FullName,
    string Email,
    string Grade,
    string Department,
    string Status);

public interface IIdentityIntegrationService
{
    Task<EmployeeProfileSummary?> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> IsEmployeeActiveAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
