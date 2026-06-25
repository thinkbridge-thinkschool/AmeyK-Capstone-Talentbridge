using FluentValidation;

namespace TalentBridge.Companies.Application.Commands.CreateCompany;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Website).MaximumLength(500).When(x => x.Website is not null);
        RuleFor(x => x.OwnerId).NotEmpty();
    }
}
