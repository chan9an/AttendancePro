using IdentityService.Common;

namespace IdentityService.Models;

/// <summary>
/// Represents an employee profile in the organization.
/// </summary>
public class Employee
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EmployeeGrade Grade { get; set; }
    public DepartmentType Department { get; set; }
    public string Designation { get; set; } = string.Empty;
    public DateTime JoiningDate { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation properties
    public virtual Employee? ReportingManager { get; set; }
    public virtual ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    public string FullName => $"{FirstName} {LastName}".Trim();

    public void UpdateProfile(string firstName, string lastName, DepartmentType department, string designation)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Department = department;
        Designation = designation.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignGrade(EmployeeGrade grade)
    {
        Grade = grade;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignReportingManager(Guid? managerId)
    {
        if (managerId.HasValue && managerId.Value == Id)
        {
            throw new DomainException("An employee cannot be assigned as their own reporting manager.");
        }

        ReportingManagerId = managerId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = EmploymentStatus.Inactive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
