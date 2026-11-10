using AttendanceManagement.Services.Attendance.DTOs;
using FluentValidation;

namespace AttendanceManagement.Services.Attendance.Validators;

public class ApplyLeaveValidator : AbstractValidator<ApplyLeaveDto>
{
    public ApplyLeaveValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId is required.");
        RuleFor(x => x.LeaveType).InclusiveBetween(1, 4).WithMessage("LeaveType must be between 1 and 4.");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required.");
        RuleFor(x => x.EndDate).NotEmpty().GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");
        RuleFor(x => x.TotalDays).GreaterThan(0).LessThanOrEqualTo(60);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class UpdateLeaveRequestValidator : AbstractValidator<UpdateLeaveRequestDto>
{
    public UpdateLeaveRequestValidator()
    {
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required.");
        RuleFor(x => x.EndDate).NotEmpty().GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");
        RuleFor(x => x.TotalDays).GreaterThan(0).LessThanOrEqualTo(60);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class ReviewLeaveValidator : AbstractValidator<ReviewLeaveDto>
{
    public ReviewLeaveValidator()
    {
        RuleFor(x => x.Remarks).MaximumLength(500);

        When(x => !x.IsApproved, () =>
        {
            RuleFor(x => x.Remarks).NotEmpty().WithMessage("Remarks are required when rejecting a leave request.");
        });
    }
}

public class ProcessMonthlyCreditValidator : AbstractValidator<ProcessMonthlyCreditDto>
{
    public ProcessMonthlyCreditValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2020, 2050);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
