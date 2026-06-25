using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Commands.UpdateJob;

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, Unit>
{
    private readonly IJobRepository _repository;
    private readonly ILogger<UpdateJobCommandHandler> _logger;

    public UpdateJobCommandHandler(IJobRepository repository, ILogger<UpdateJobCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found.");

        if (job.PostedByHRId != request.RequestedByHRId)
            throw new UnauthorizedAccessException("You can only edit your own jobs.");

        var result = job.Update(request.Title, request.Description, request.Location, request.SalaryMin, request.SalaryMax);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[Jobs] Job {JobId} updated by HR {HRId}", request.JobId, request.RequestedByHRId);

        return Unit.Value;
    }
}
