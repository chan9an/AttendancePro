namespace AttendanceManagement.Services.Identity.Domain.Enums;

/// <summary>
/// Defines employee organizational grades used to determine monthly leave entitlements.
/// </summary>
public enum EmployeeGrade
{
    /// <summary>
    /// Junior / Associate grade. Monthly entitlement: 1.5 days.
    /// </summary>
    GradeC = 1,

    /// <summary>
    /// Mid-level / Senior grade. Monthly entitlement: 2.0 days.
    /// </summary>
    GradeB = 2,

    /// <summary>
    /// Lead / Principal / Executive grade. Monthly entitlement: 2.5 days.
    /// </summary>
    GradeA = 3
}
