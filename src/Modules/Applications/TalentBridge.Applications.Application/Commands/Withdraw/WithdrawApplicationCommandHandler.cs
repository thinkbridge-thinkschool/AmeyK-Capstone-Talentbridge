using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Domain.Entities;

namespace TalentBridge.Applications.Application.Commands.Withdraw;

public class WithdrawApplicationCommandHandler : IRequestHandler<WithdrawApplicationCommand, Unit>
{
    private readonly IApplicationsDbContext _dbContext;
    private readonly ILogger<WithdrawApplicationCommandHandler> _logger;

    public WithdrawApplicationCommandHandler(IApplicationsDbContext dbContext, ILogger<WithdrawApplicationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(WithdrawApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _dbContext.JobApplications.FindAsync([request.ApplicationId], cancellationToken)
            ?? throw new KeyNotFoundException($"Application {request.ApplicationId} not found.");

        if (application.CandidateId != request.CandidateId)
            throw new UnauthorizedAccessException("You can only withdraw your own applications.");

        var previousStatus = application.Status.ToString();
        var result = application.Withdraw();
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);

        var history = ApplicationStatusHistory.Create(
            application.Id, previousStatus, "Withdrawn", request.CandidateId);
        await _dbContext.StatusHistory.AddAsync(history, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Applications] Application {Id} withdrawn by candidate {CandidateId}",
            application.Id, request.CandidateId);

        return Unit.Value;
    }
}
