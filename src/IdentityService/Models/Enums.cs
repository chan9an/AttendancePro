namespace IdentityService.Models;

/// <summary>
/// Organizational seniority tier determining leave entitlement rates.
/// </summary>
public enum EmployeeGrade
{
    GradeC = 1, // Associate / Junior: 1.5 days/month
    GradeB = 2, // Mid-level / Senior: 2.0 days/month
    GradeA = 3  // Lead / Principal: 2.5 days/month
}

/// <summary>
/// Organizational department departments.
/// </summary>
public enum DepartmentType
{
    Engineering = 1,
    HumanResources = 2,
    Finance = 3,
    Operations = 4,
    QualityAssurance = 5
}

/// <summary>
/// Lifecycle status of an employee profile.
/// </summary>
public enum EmploymentStatus
{
    Active = 1,
    Inactive = 2,
    OnLeave = 3,
    Terminated = 4
}
