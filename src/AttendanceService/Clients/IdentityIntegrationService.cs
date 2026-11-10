using System.Net;
using System.Net.Http.Json;
using AttendanceService.Common;

namespace AttendanceService.Clients;

/// <summary>
/// Typed HTTP Client communicating with the IdentityService.
/// Strictly uses HTTP communication; never accesses the Identity database directly.
/// Handles HTTP 404 as employee-not-found (returns null).
/// Translates HTTP 5xx, connection failures, and genuine timeouts into ServiceUnavailableException (HTTP 503).
/// Preserves caller-requested cancellation by allowing OperationCanceledException to propagate untranslated.
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

    public async Task<EmployeeProfileSummary?> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/employees/{employeeId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Employee {EmployeeId} was not found in Identity Service.", employeeId);
                return null;
            }

            if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout)
            {
                _logger.LogError("Identity service returned server error status {StatusCode} for Employee {EmployeeId}", response.StatusCode, employeeId);
                throw new ServiceUnavailableException("Identity Service", $"HTTP {(int)response.StatusCode}: Identity Service is experiencing issues.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Identity service returned client error status {StatusCode} for Employee {EmployeeId}", response.StatusCode, employeeId);
                throw new DomainException($"Identity service returned HTTP {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<EmployeeProfileSummary>(cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller-requested cancellation: do NOT translate to 503; let ASP.NET Core pipeline handle client disconnect
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Request timed out while contacting Identity Service for Employee {EmployeeId}", employeeId);
            throw new ServiceUnavailableException("Identity Service", "Request to Identity Service timed out.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request timed out while contacting Identity Service for Employee {EmployeeId}", employeeId);
            throw new ServiceUnavailableException("Identity Service", "Request to Identity Service timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network connectivity failure while contacting Identity Service for Employee {EmployeeId}", employeeId);
            throw new ServiceUnavailableException("Identity Service", $"Connection failed: {ex.Message}");
        }
    }

    public async Task<bool> IsEmployeeActiveAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var profile = await GetEmployeeProfileAsync(employeeId, cancellationToken);
        return profile != null && string.Equals(profile.Status, "Active", StringComparison.OrdinalIgnoreCase);
    }
}
