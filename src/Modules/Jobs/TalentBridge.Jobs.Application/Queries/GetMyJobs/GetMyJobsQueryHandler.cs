using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Application.DTOs;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Queries.GetMyJobs;

public class GetMyJobsQueryHandler : IRequestHandler<GetMyJobsQuery, List<JobDto>>
{
    private readonly IJobRepository _repository;
    private readonly ILogger<GetMyJobsQueryHandler> _logger;

    public GetMyJobsQueryHandler(IJobRepository repository, ILogger<GetMyJobsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<JobDto>> Handle(GetMyJobsQuery request, CancellationToken cancellationToken)
    {
        var jobs = await _repository.GetByHRIdAsync(request.HRId, cancellationToken);

        _logger.LogInformation("[Jobs] HR {HRId} fetched {Count} own jobs", request.HRId, jobs.Count);

        return jobs.Select(j => new JobDto(
            j.Id, j.CompanyId, j.PostedByHRId,
            j.Title, j.Description, j.Location,
            j.SalaryMin, j.SalaryMax, j.Status,
            j.CreatedAtUtc, j.PublishedAtUtc, j.ExpiresAtUtc)).ToList();
    }
}
