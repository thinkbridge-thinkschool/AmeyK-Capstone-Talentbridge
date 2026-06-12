using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TalentBridge.Jobs.Application.DTOs;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Queries.GetJobById;

public class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDto?>
{
    private readonly IJobRepository _repository;
    private readonly HybridCache _cache;
    private readonly ILogger<GetJobByIdQueryHandler> _logger;

    public GetJobByIdQueryHandler(IJobRepository repository, HybridCache cache, ILogger<GetJobByIdQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<JobDto?> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"job:{request.JobId}";

        var cached = await _cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogInformation("[Jobs Cache MISS] job:{JobId}", request.JobId);
                var job = await _repository.GetByIdAsync(request.JobId, ct);
                if (job is null) return null;

                return new JobDto(
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
                    [.. job.RequiredSkills]);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            },
            cancellationToken: cancellationToken);

        if (cached is not null)
            _logger.LogInformation("[Jobs Cache HIT] job:{JobId}", request.JobId);

        return cached;
    }
}
