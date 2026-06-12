using FluentValidation;

namespace TalentBridge.Applications.Application.Commands.Apply;

public class ApplyCommandValidator : AbstractValidator<ApplyCommand>
{
    public ApplyCommandValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.CoverLetter).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.ResumeUrl).NotEmpty();
    }
}
