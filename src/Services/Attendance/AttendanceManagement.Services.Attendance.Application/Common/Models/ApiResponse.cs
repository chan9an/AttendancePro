namespace AttendanceManagement.Services.Attendance.Application.Common.Models;

/// <summary>
/// Standard enterprise API response wrapper.
/// Guarantees consistent response schema across all microservices.
/// </summary>
public class ApiResponse<T>
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Request completed successfully.")
    {
        return new ApiResponse<T>
        {
            Succeeded = true,
            Message = message,
            Data = data,
            Errors = null
        };
    }

    public static ApiResponse<T> Failure(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Succeeded = false,
            Message = message,
            Data = default,
            Errors = errors ?? new List<string>()
        };
    }
}
