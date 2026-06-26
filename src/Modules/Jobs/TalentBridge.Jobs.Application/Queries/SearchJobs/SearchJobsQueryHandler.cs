using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TalentBridge.Jobs.Application.DTOs;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Queries.SearchJobs;

public class SearchJobsQueryHandler : IRequestHandler<SearchJobsQuery, PagedResult<JobDto>>
{
    private readonly IJobRepository _repository;
    private readonly HybridCache _cache;

    public SearchJobsQueryHandler(IJobRepository repository, HybridCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<PagedResult<JobDto>> Handle(SearchJobsQuery request, CancellationToken cancellationToken)
    {
        var allCacheKey = $"jobs:search:{request.Keyword}:{request.Location}:{request.SalaryMin}:{request.SalaryMax}";

        var allJobs = await _cache.GetOrCreateAsync(
            allCacheKey,
            async ct =>
            {
                var jobs = await _repository.SearchAsync(
                    request.Keyword ?? string.Empty,
                    request.Location,
                    request.SalaryMin,
                    request.SalaryMax,
                    ct);

                return jobs.Select(job => new JobDto(
                    job.Id,
                    job.CompanyId,
                    job.PostedByHRId,
                    job.Title,
                    job.Description,
                    job.Location,
                    job.SalaryMin,
                    job.SalaryMax,
                    job.Status,
                    job.CreatedAtUtc,
                    job.PublishedAtUtc,
                    job.ExpiresAtUtc)).ToList();
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            tags: ["jobs"],
            cancellationToken: cancellationToken);

        var all = allJobs ?? [];
        var totalCount = all.Count;
        var page = Math.Max(1, request.Page);
        var size = Math.Max(1, request.Size);
        var items = all.Skip((page - 1) * size).Take(size).ToList();

        return new PagedResult<JobDto>(items, totalCount);
    }
}
