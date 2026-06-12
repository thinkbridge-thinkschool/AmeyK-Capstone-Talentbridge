using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Commands.CloseJob;

public class CloseJobCommandHandler : IRequestHandler<CloseJobCommand, Unit>
{
    private readonly IJobRepository _repository;
    private readonly ILogger<CloseJobCommandHandler> _logger;

    public CloseJobCommandHandler(IJobRepository repository, ILogger<CloseJobCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Unit> Handle(CloseJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found.");

        if (job.CompanyId != request.RequestingCompanyId)
            throw new UnauthorizedAccessException("You do not own this job posting.");

        job.Close();

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Jobs] Job {JobId} closed by company {CompanyId}", job.Id, request.RequestingCompanyId);

        return Unit.Value;
    }
}
