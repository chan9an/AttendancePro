using AttendanceManagement.Services.Attendance.DTOs;
using FluentValidation;

namespace AttendanceManagement.Services.Attendance.Validators;

public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Code)
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

public class UpdateProjectValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Code)
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

public class AssignEmployeeProjectValidator : AbstractValidator<AssignEmployeeProjectDto>
{
    public AssignEmployeeProjectValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("EmployeeId is required.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.");

        When(x => x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate!.Value)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("EndDate must be greater than or equal to StartDate.");
        });
    }
}
