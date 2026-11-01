namespace AttendanceManagement.Services.Attendance.Domain.Exceptions;

/// <summary>
/// Exception thrown when an Attendance or Leave domain business rule is violated.
/// </summary>
public class AttendanceDomainException : Exception
{
    public AttendanceDomainException()
    {
    }

    public AttendanceDomainException(string message)
        : base(message)
    {
    }

    public AttendanceDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
