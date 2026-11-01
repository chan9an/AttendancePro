using AttendanceManagement.Services.Identity.Domain.Enums;
using AttendanceManagement.Services.Identity.Domain.Exceptions;

namespace AttendanceManagement.Services.Identity.Domain.Entities;

/// <summary>
/// Represents an employee entity within the enterprise organization.
/// Serves as the primary business profile associated with an identity user account.
/// </summary>
public class Employee
{
    /// <summary>
    /// Unique identifier for the employee record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Foreign key reference to the underlying ASP.NET Core Identity user account (AspNetUsers.Id).
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Organizational grade dictating seniority and leave entitlements.
    /// </summary>
    public EmployeeGrade Grade { get; private set; }

    public DepartmentType Department { get; private set; }
    public string Designation { get; private set; } = string.Empty;
    public DateTime JoiningDate { get; private set; }

    /// <summary>
    /// Optional self-referencing foreign key pointing to the reporting manager's Employee Id.
    /// </summary>
    public Guid? ReportingManagerId { get; private set; }

    /// <summary>
    /// Navigation property to the employee's direct reporting manager.
    /// </summary>
    public virtual Employee? ReportingManager { get; private set; }

    /// <summary>
    /// Navigation collection of employees who report directly to this employee.
    /// </summary>
    public virtual ICollection<Employee> Subordinates { get; private set; } = new List<Employee>();

    public EmploymentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// </summary>
    protected Employee()
    {
    }

    /// <summary>
    /// Factory method for creating a valid new employee record.
    /// Enforces business invariant validation upon instantiation.
    /// </summary>
    public static Employee Create(
        Guid id,
        string userId,
        string firstName,
        string lastName,
        string email,
        EmployeeGrade grade,
        DepartmentType department,
        string designation,
        DateTime joiningDate,
        Guid? reportingManagerId)
    {
        if (id == Guid.Empty)
            throw new IdentityDomainException("Employee Id cannot be empty.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new IdentityDomainException("Associated Identity UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new IdentityDomainException("First name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new IdentityDomainException("Last name is required.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new IdentityDomainException("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(designation))
            throw new IdentityDomainException("Designation is required.");

        return new Employee
        {
            Id = id,
            UserId = userId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Grade = grade,
            Department = department,
            Designation = designation.Trim(),
            JoiningDate = joiningDate,
            ReportingManagerId = reportingManagerId,
            Status = EmploymentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates employee organizational details.
    /// </summary>
    public void UpdateProfile(
        string firstName,
        string lastName,
        DepartmentType department,
        string designation)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new IdentityDomainException("First name cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new IdentityDomainException("Last name cannot be empty.");

        if (string.IsNullOrWhiteSpace(designation))
            throw new IdentityDomainException("Designation cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Department = department;
        Designation = designation.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns or updates the employee's organizational grade.
    /// </summary>
    public void AssignGrade(EmployeeGrade newGrade)
    {
        Grade = newGrade;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns the employee to a new reporting manager.
    /// Enforces rule: an employee cannot be their own manager.
    /// </summary>
    public void AssignReportingManager(Guid? newManagerId)
    {
        if (newManagerId.HasValue && newManagerId.Value == Id)
            throw new IdentityDomainException("An employee cannot be assigned as their own reporting manager.");

        ReportingManagerId = newManagerId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes the status of an employee (e.g. Deactivation / Termination).
    /// </summary>
    public void UpdateStatus(EmploymentStatus newStatus)
    {
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
