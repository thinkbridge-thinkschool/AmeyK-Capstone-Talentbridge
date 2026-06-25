using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Commands.DeleteJob;

public class DeleteJobCommandHandler : IRequestHandler<DeleteJobCommand, Unit>
{
    private readonly IJobRepository _repository;
    private readonly ILogger<DeleteJobCommandHandler> _logger;

    public DeleteJobCommandHandler(IJobRepository repository, ILogger<DeleteJobCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found.");

        if (job.PostedByHRId != request.RequestedByHRId)
            throw new UnauthorizedAccessException("You can only delete your own jobs.");

        await _repository.DeleteAsync(request.JobId, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Jobs] Job {JobId} deleted by HR {HRId}", request.JobId, request.RequestedByHRId);
        return Unit.Value;
    }
}
