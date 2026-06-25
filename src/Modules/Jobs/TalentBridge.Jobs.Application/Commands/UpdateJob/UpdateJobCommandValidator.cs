using FluentValidation;

namespace TalentBridge.Jobs.Application.Commands.UpdateJob;

public class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.SalaryMin).GreaterThan(0);
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(x => x.SalaryMin);
    }
}
