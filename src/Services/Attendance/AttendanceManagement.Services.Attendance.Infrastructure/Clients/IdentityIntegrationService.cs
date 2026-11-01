using System.Net.Http.Json;
using AttendanceManagement.Services.Attendance.Application.Common.Models;
using AttendanceManagement.Services.Attendance.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace AttendanceManagement.Services.Attendance.Infrastructure.Clients;

/// <summary>
/// Typed HTTP Client communicating with the Identity & Employee microservice.
/// Uses IHttpClientFactory managed lifetimes to prevent socket exhaustion.
/// </summary>
public class IdentityIntegrationService : IIdentityIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityIntegrationService> _logger;

    public IdentityIntegrationService(HttpClient httpClient, ILogger<IdentityIntegrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmployeeProfileSummaryDto?> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/employees/{employeeId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve employee profile {EmployeeId}. StatusCode: {Status}", employeeId, response.StatusCode);
                return null;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeProfileSummaryDto>>(cancellationToken: cancellationToken);
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP call to Identity Service failed while querying employee {EmployeeId}", employeeId);
            return null;
        }
    }

    public async Task<bool> IsEmployeeActiveAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var profile = await GetEmployeeProfileAsync(employeeId, cancellationToken);
        return profile != null && string.Equals(profile.Status, "Active", StringComparison.OrdinalIgnoreCase);
    }
}
