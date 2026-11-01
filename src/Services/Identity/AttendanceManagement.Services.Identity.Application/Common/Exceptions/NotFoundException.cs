namespace AttendanceManagement.Services.Identity.Application.Common.Exceptions;

/// <summary>
/// Thrown when an expected domain or persistence record is not found.
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
