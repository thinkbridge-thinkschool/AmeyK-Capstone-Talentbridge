using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Application.DTOs;
using TalentBridge.Jobs.Domain.Enums;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Queries.SearchJobs;

public class SearchJobsQueryHandler : IRequestHandler<SearchJobsQuery, List<JobDto>>
{
    private readonly IJobRepository _repository;
    private readonly HybridCache _cache;
    private readonly ILogger<SearchJobsQueryHandler> _logger;

    public SearchJobsQueryHandler(IJobRepository repository, HybridCache cache, ILogger<SearchJobsQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<JobDto>> Handle(SearchJobsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"jobs:search:{request.Keyword}:{request.Location}:{request.JobType}:{request.Page}:{request.Size}";

        var result = await _cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                JobType? jobType = null;
                if (!string.IsNullOrWhiteSpace(request.JobType) && Enum.TryParse<JobType>(request.JobType, true, out var parsed))
                    jobType = parsed;

                var jobs = await _repository.SearchAsync(
                    request.Keyword ?? string.Empty,
                    request.Location,
                    jobType,
                    ct);

                return jobs.Select(job => new JobDto(
                    job.Id,
                    job.CompanyId,
                    job.Title,
                    job.Description,
                    job.Location,
                    job.SalaryMin,
                    job.SalaryMax,
                    job.Status,
                    job.Type,
                    job.ClosingDate,
                    job.CreatedAt,
                    [.. job.RequiredSkills])).ToList();
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        return result ?? [];
    }
}
