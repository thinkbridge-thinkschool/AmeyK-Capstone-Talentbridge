using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Domain.Aggregates;

namespace TalentBridge.Applications.Application.Queries.GetApplication;

public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, JobApplication?>
{
    private readonly IApplicationsDbContext _dbContext;
    private readonly HybridCache _cache;
    private readonly ILogger<GetApplicationByIdQueryHandler> _logger;

    public GetApplicationByIdQueryHandler(IApplicationsDbContext dbContext, HybridCache cache, ILogger<GetApplicationByIdQueryHandler> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<JobApplication?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"application:{request.ApplicationId}";

        var result = await _cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogInformation("[Applications Cache MISS] application:{Id}", request.ApplicationId);
                return await _dbContext.JobApplications.FindAsync([request.ApplicationId], ct);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);

        if (result is not null)
            _logger.LogInformation("[Applications Cache HIT] application:{Id}", request.ApplicationId);

        return result;
    }
}
