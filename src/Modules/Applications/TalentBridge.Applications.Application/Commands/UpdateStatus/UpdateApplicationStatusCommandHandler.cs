using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Applications.Application.Commands.UpdateStatus;

public class UpdateApplicationStatusCommandHandler : IRequestHandler<UpdateApplicationStatusCommand, Unit>
{
    private readonly IApplicationsDbContext _dbContext;
    private readonly ILogger<UpdateApplicationStatusCommandHandler> _logger;

    public UpdateApplicationStatusCommandHandler(IApplicationsDbContext dbContext, ILogger<UpdateApplicationStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var application = await _dbContext.JobApplications.FindAsync([request.ApplicationId], cancellationToken)
            ?? throw new KeyNotFoundException($"Application {request.ApplicationId} not found.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // HR id placeholder — in a real implementation this comes from ICurrentUserService
            var hrId = Guid.Empty;

            var operationResult = request.NewStatus.ToLowerInvariant() switch
            {
                "underreview" => application.StartReview(hrId),
                "shortlisted" => application.Shortlist(hrId),
                "accepted" => application.Accept(hrId),
                "rejected" => application.Reject(hrId, request.RejectionReason ?? string.Empty),
                "withdrawn" => application.Withdraw(),
                _ => throw new ArgumentException($"Invalid status transition: {request.NewStatus}")
            };

            if (operationResult.IsFailure)
                throw new InvalidOperationException(operationResult.Error);

            var outboxMessage = new OutboxMessage
            {
                Type = $"Application{request.NewStatus}",
                Payload = JsonSerializer.Serialize(new
                {
                    ApplicationId = application.Id,
                    application.CandidateId,
                    application.JobId,
                    NewStatus = request.NewStatus,
                    RejectionReason = request.RejectionReason
                }),
                OccurredOnUtc = DateTime.UtcNow
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Applications] Application {Id} status updated to {Status} + outbox row committed atomically",
                application.Id, request.NewStatus);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "[Applications] Failed to update application status — rolled back");
            throw;
        }
    }
}
