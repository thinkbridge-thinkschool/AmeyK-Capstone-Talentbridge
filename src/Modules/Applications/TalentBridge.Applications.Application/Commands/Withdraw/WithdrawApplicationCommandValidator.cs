using FluentValidation;

namespace TalentBridge.Applications.Application.Commands.Withdraw;

public class WithdrawApplicationCommandValidator : AbstractValidator<WithdrawApplicationCommand>
{
    public WithdrawApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
    }
}
