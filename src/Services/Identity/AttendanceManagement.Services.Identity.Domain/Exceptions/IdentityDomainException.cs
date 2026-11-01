namespace AttendanceManagement.Services.Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when an Identity or Employee domain business rule is violated.
/// </summary>
public class IdentityDomainException : Exception
{
    public IdentityDomainException()
    {
    }

    public IdentityDomainException(string message)
        : base(message)
    {
    }

    public IdentityDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
