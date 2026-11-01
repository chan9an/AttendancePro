namespace AttendanceManagement.Services.Attendance.Application.Common.Exceptions;

/// <summary>
/// Thrown when an action violates an enterprise business rule
/// (e.g. attempting to log attendance on a day with approved leave, or applying for leave with insufficient balance).
/// </summary>
public class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }

    public BusinessRuleViolationException(string ruleName, string message)
        : base($"Business rule '{ruleName}' was violated: {message}")
    {
    }
}

/// <summary>
/// Thrown when an expected entity cannot be found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with identifier '({key})' was not found.")
    {
    }
}

/// <summary>
/// Thrown when request payload fails FluentValidation.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}
