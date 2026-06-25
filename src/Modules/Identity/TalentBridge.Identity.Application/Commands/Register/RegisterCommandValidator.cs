using FluentValidation;

namespace TalentBridge.Identity.Application.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private static readonly string[] ValidRoles = ["Candidate", "CompanyHR", "Admin"];

    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => ValidRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be one of: Candidate, CompanyHR, Admin.");

        RuleFor(x => x.FullName).MaximumLength(200);
    }
}
