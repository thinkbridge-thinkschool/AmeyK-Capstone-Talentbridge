using FluentValidation;

namespace TalentBridge.Jobs.Application.Commands.PostJob;

public class PostJobCommandValidator : AbstractValidator<PostJobCommand>
{
    public PostJobCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.SalaryMin).GreaterThan(0);
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(x => x.SalaryMin);
        RuleFor(x => x.Location).NotEmpty();
    }
}
