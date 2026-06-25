using FluentValidation;

namespace TalentBridge.Applications.Application.Commands.UpdateStatus;

public class UpdateApplicationStatusCommandValidator : AbstractValidator<UpdateApplicationStatusCommand>
{
    private static readonly string[] ValidStatuses =
        ["Submitted", "UnderReview", "Shortlisted", "Accepted", "Rejected", "Withdrawn"];

    public UpdateApplicationStatusCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.NewStatus)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"NewStatus must be one of: {string.Join(", ", ValidStatuses)}");
        RuleFor(x => x.RejectionReason)
            .MaximumLength(1000)
            .When(x => x.RejectionReason is not null);
    }
}
