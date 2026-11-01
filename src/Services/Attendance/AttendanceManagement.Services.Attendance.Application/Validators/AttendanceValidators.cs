using AttendanceManagement.Services.Attendance.Application.DTOs.Attendance;
using FluentValidation;

namespace AttendanceManagement.Services.Attendance.Application.Validators;

public class CreateAttendanceValidator : AbstractValidator<CreateAttendanceDto>
{
    public CreateAttendanceValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("EmployeeId is required.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Attendance date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Cannot log attendance for future dates.");

        RuleFor(x => x.Remarks)
            .MaximumLength(500);
    }
}

public class UpdateAttendanceValidator : AbstractValidator<UpdateAttendanceDto>
{
    public UpdateAttendanceValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Remarks)
            .MaximumLength(500);
    }
}
