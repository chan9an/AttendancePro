namespace AttendanceManagement.Services.Identity.Common;

/// <summary>
/// Thrown when a requested resource is not found in the database.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Resource '{entityName}' with key ({key}) was not found.")
    {
    }
}

/// <summary>
/// Thrown when an operation violates uniqueness or business state conflicts.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when input payload fails validation rules.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}

/// <summary>
/// Thrown when a business rule or invariant is violated.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
