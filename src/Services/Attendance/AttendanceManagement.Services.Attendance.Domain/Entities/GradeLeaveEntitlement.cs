using AttendanceManagement.Services.Attendance.Domain.Exceptions;

namespace AttendanceManagement.Services.Attendance.Domain.Entities;

/// <summary>
/// Configurable grade-based leave entitlement policy.
/// Dictates monthly leave accrual rates and annual caps per grade.
/// </summary>
public class GradeLeaveEntitlement
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Employee grade numeric identifier (1: GradeC, 2: GradeB, 3: GradeA).
    /// </summary>
    public int Grade { get; private set; }

    public string GradeName { get; private set; } = string.Empty;

    /// <summary>
    /// Days accrued per calendar month.
    /// E.g. Grade A: 2.50 days, Grade B: 2.00 days, Grade C: 1.50 days.
    /// </summary>
    public decimal MonthlyCreditDays { get; private set; }

    /// <summary>
    /// Maximum annual leave credit accumulation allowed.
    /// </summary>
    public decimal AnnualMaximumCap { get; private set; }

    protected GradeLeaveEntitlement()
    {
    }

    public static GradeLeaveEntitlement Create(
        int grade,
        string gradeName,
        decimal monthlyCreditDays,
        decimal annualMaximumCap)
    {
        if (grade < 1 || grade > 3)
            throw new AttendanceDomainException("Invalid employee grade.");

        if (monthlyCreditDays <= 0)
            throw new AttendanceDomainException("Monthly credit days must be positive.");

        if (annualMaximumCap < monthlyCreditDays)
            throw new AttendanceDomainException("Annual cap cannot be lower than single month credit.");

        return new GradeLeaveEntitlement
        {
            Id = Guid.NewGuid(),
            Grade = grade,
            GradeName = gradeName.Trim(),
            MonthlyCreditDays = monthlyCreditDays,
            AnnualMaximumCap = annualMaximumCap
        };
    }

    public void UpdatePolicy(decimal monthlyCreditDays, decimal annualMaximumCap)
    {
        if (monthlyCreditDays <= 0)
            throw new AttendanceDomainException("Monthly credit days must be positive.");

        MonthlyCreditDays = monthlyCreditDays;
        AnnualMaximumCap = annualMaximumCap;
    }
}
