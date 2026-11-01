namespace AttendanceManagement.Services.Attendance.Infrastructure.Clients;

/// <summary>
/// Strongly-typed configuration options for internal microservice endpoint URLs.
/// </summary>
public class ServiceEndpoints
{
    public const string SectionName = "ServiceEndpoints";

    public string IdentityServiceBaseUrl { get; set; } = "http://localhost:5001";
}
