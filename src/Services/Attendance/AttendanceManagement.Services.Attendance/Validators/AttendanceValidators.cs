using AttendanceManagement.Services.Attendance.DTOs;
using FluentValidation;

namespace AttendanceManagement.Services.Attendance.Validators;

public class CreateAttendanceValidator : AbstractValidator<CreateAttendanceDto>
{
    public CreateAttendanceValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId is required.");
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("ProjectId is required.");
        RuleFor(x => x.ProjectName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Date).NotEmpty().LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Cannot log attendance for future dates.");
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class UpdateAttendanceValidator : AbstractValidator<UpdateAttendanceDto>
{
    public UpdateAttendanceValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("ProjectId is required.");
        RuleFor(x => x.ProjectName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
