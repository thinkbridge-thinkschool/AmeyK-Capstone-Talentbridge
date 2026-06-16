using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Domain.Aggregates;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Applications.Application.Commands.Apply;

public class ApplyCommandHandler : IRequestHandler<ApplyCommand, ApplyResult>
{
    private readonly IApplicationsDbContext _dbContext;
    private readonly ILogger<ApplyCommandHandler> _logger;

    public ApplyCommandHandler(IApplicationsDbContext dbContext, ILogger<ApplyCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApplyResult> Handle(ApplyCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = JobApplication.Create(
                request.CandidateId,
                request.JobId,
                request.CoverLetter,
                request.ResumeUrl);

            if (result.IsFailure)
                throw new InvalidOperationException(result.Error);

            var application = result.Value!;

            await _dbContext.JobApplications.AddAsync(application, cancellationToken);

            var outboxMessage = new OutboxMessage
            {
                Type = "ApplicationSubmitted",
                Payload = JsonSerializer.Serialize(new
                {
                    ApplicationId = application.Id,
                    request.JobId,
                    request.CandidateId,
                    application.SubmittedAtUtc
                }),
                OccurredOnUtc = application.SubmittedAtUtc
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Applications] Application {Id} + outbox row committed atomically",
                application.Id);

            return new ApplyResult(application.Id, application.Status.ToString());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "[Applications] Failed to create application — rolled back");
            throw;
        }
    }
}
