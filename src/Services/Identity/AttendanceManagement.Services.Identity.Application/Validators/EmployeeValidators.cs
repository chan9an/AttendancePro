using AttendanceManagement.Services.Identity.Application.DTOs.Employee;
using FluentValidation;

namespace AttendanceManagement.Services.Identity.Application.Validators;

public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Must be a valid email.");

        RuleFor(x => x.Grade)
            .InclusiveBetween(1, 3).WithMessage("Invalid employee grade.");

        RuleFor(x => x.Department)
            .InclusiveBetween(1, 5).WithMessage("Invalid department.");

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(100);

        RuleFor(x => x.JoiningDate)
            .NotEmpty().WithMessage("Joining date is required.");
    }
}

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Department)
            .InclusiveBetween(1, 5).WithMessage("Invalid department.");

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(100);
    }
}
