using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Commands.PublishJob;

public class PublishJobCommandHandler : IRequestHandler<PublishJobCommand, Unit>
{
    private readonly IJobRepository _repository;
    private readonly HybridCache _cache;
    private readonly ILogger<PublishJobCommandHandler> _logger;

    public PublishJobCommandHandler(IJobRepository repository, HybridCache cache, ILogger<PublishJobCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(PublishJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found.");

        if (job.PostedByHRId != request.RequestingHRId)
            throw new UnauthorizedAccessException("You do not own this job posting.");

        var result = job.Publish();
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);

        await _repository.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByTagAsync("jobs", cancellationToken);

        _logger.LogInformation("[Jobs] Job {JobId} published by HR {HRId}", job.Id, request.RequestingHRId);

        return Unit.Value;
    }
}
