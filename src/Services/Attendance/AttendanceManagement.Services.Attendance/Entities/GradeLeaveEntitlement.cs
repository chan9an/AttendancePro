namespace AttendanceManagement.Services.Attendance.Entities;

/// <summary>
/// Configurable grade-based leave entitlement policy.
/// Dictates monthly leave accrual rates and annual caps per grade.
/// </summary>
public class GradeLeaveEntitlement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Grade { get; set; }
    public string GradeName { get; set; } = string.Empty;
    public decimal MonthlyCreditDays { get; set; }
    public decimal AnnualMaximumCap { get; set; }

    public static GradeLeaveEntitlement Create(int grade, string gradeName, decimal monthlyCreditDays, decimal annualMaximumCap)
    {
        return new GradeLeaveEntitlement
        {
            Id = Guid.NewGuid(),
            Grade = grade,
            GradeName = gradeName.Trim(),
            MonthlyCreditDays = monthlyCreditDays,
            AnnualMaximumCap = annualMaximumCap
        };
    }
}
