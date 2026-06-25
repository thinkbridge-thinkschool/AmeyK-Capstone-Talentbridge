using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Domain.Entities;
using TalentBridge.Shared.Interfaces;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Applications.Application.Commands.UpdateStatus;

public class UpdateApplicationStatusCommandHandler : IRequestHandler<UpdateApplicationStatusCommand, Unit>
{
    private readonly IApplicationsDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UpdateApplicationStatusCommandHandler> _logger;

    public UpdateApplicationStatusCommandHandler(
        IApplicationsDbContext dbContext,
        ICurrentUserService currentUser,
        INotificationService notificationService,
        ILogger<UpdateApplicationStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var application = await _dbContext.JobApplications.FindAsync([request.ApplicationId], cancellationToken)
            ?? throw new KeyNotFoundException($"Application {request.ApplicationId} not found.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var hrId = _currentUser.UserId;
            var previousStatus = application.Status.ToString();

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

            var historyEntry = ApplicationStatusHistory.Create(
                applicationId: application.Id,
                fromStatus: previousStatus,
                toStatus: request.NewStatus,
                changedByUserId: hrId,
                notes: request.RejectionReason);
            await _dbContext.StatusHistory.AddAsync(historyEntry, cancellationToken);

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

            var statusMessage = request.NewStatus.ToLowerInvariant() switch
            {
                "underreview" => "Your application is now under review by the hiring team.",
                "shortlisted" => "Congratulations! You have been shortlisted for the position.",
                "accepted"    => "Great news! Your application has been accepted.",
                "rejected"    => "Your application has been reviewed. Unfortunately, it was not selected at this time.",
                "withdrawn"   => "Your application has been withdrawn.",
                _             => $"Your application status has been updated to {request.NewStatus}."
            };
            await _notificationService.CreateAsync(application.CandidateId, statusMessage, cancellationToken);

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
