using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Domain.Aggregates;
using TalentBridge.Jobs.Domain.Enums;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Commands.PostJob;

public class PostJobCommandHandler : IRequestHandler<PostJobCommand, PostJobResult>
{
    private readonly IJobRepository _repository;
    private readonly ILogger<PostJobCommandHandler> _logger;

    public PostJobCommandHandler(IJobRepository repository, ILogger<PostJobCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PostJobResult> Handle(PostJobCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<JobType>(request.JobType, true, out var jobType))
            throw new ArgumentException($"Invalid job type: {request.JobType}");

        var job = Job.Create(
            request.CompanyId,
            request.Title,
            request.Description,
            request.Location,
            request.SalaryMin,
            request.SalaryMax,
            jobType,
            request.RequiredSkills);

        await _repository.AddAsync(job, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Jobs] Job {JobId} created by company {CompanyId}", job.Id, request.CompanyId);

        return new PostJobResult(job.Id, job.Status.ToString());
    }
}
