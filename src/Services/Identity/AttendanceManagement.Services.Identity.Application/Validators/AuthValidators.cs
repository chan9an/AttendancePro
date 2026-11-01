using AttendanceManagement.Services.Identity.Application.DTOs.Auth;
using FluentValidation;

namespace AttendanceManagement.Services.Identity.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid corporate email address must be provided.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class RegisterEmployeeValidator : AbstractValidator<RegisterEmployeeDto>
{
    public RegisterEmployeeValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one numerical digit.")
            .Matches(@"[\!\?\*\@\#\$\%\^\&\+\=]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Grade)
            .InclusiveBetween(1, 3).WithMessage("Grade must be a valid grade enum value (1: GradeC, 2: GradeB, 3: GradeA).");

        RuleFor(x => x.Department)
            .InclusiveBetween(1, 5).WithMessage("Department must be a valid department enum value (1-5).");

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(100).WithMessage("Designation cannot exceed 100 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role is "Administrator" or "Manager" or "Employee")
            .WithMessage("Role must be one of: Administrator, Manager, Employee.");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Expired access token is required for validation.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
