using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Commands.PublishJob;

public class PublishJobCommandHandler : IRequestHandler<PublishJobCommand, Unit>
{
    private readonly IJobRepository _repository;
    private readonly ILogger<PublishJobCommandHandler> _logger;

    public PublishJobCommandHandler(IJobRepository repository, ILogger<PublishJobCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Unit> Handle(PublishJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found.");

        if (job.CompanyId != request.RequestingCompanyId)
            throw new UnauthorizedAccessException("You do not own this job posting.");

        job.Publish();

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Jobs] Job {JobId} published by company {CompanyId}", job.Id, request.RequestingCompanyId);

        return Unit.Value;
    }
}
