using AttendanceManagement.Services.Attendance.Application.DTOs.Leave;
using FluentValidation;

namespace AttendanceManagement.Services.Attendance.Application.Validators;

public class ApplyLeaveValidator : AbstractValidator<ApplyLeaveDto>
{
    public ApplyLeaveValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("EmployeeId is required.");

        RuleFor(x => x.LeaveType)
            .InclusiveBetween(1, 4)
            .WithMessage("LeaveType must be between 1 (Annual) and 4 (Unpaid).");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.TotalDays)
            .GreaterThan(0)
            .WithMessage("Total leave days must be greater than zero.")
            .LessThanOrEqualTo(60)
            .WithMessage("Cannot submit a single leave request exceeding 60 consecutive days.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason for leave is required.")
            .MaximumLength(500);
    }
}

public class ReviewLeaveValidator : AbstractValidator<ReviewLeaveDto>
{
    public ReviewLeaveValidator()
    {
        RuleFor(x => x.Remarks)
            .MaximumLength(500);

        // If rejecting, comments are mandatory for transparency
        When(x => !x.IsApproved, () =>
        {
            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Remarks are required when rejecting a leave request.");
        });
    }
}

public class ProcessMonthlyCreditValidator : AbstractValidator<ProcessMonthlyCreditDto>
{
    public ProcessMonthlyCreditValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2020, 2050)
            .WithMessage("Year must be valid.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12.");
    }
}
