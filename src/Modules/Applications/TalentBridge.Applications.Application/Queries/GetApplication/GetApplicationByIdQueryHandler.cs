using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Domain.Aggregates;

namespace TalentBridge.Applications.Application.Queries.GetApplication;

public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, JobApplication?>
{
    private readonly IApplicationsDbContext _dbContext;
    private readonly ILogger<GetApplicationByIdQueryHandler> _logger;

    public GetApplicationByIdQueryHandler(IApplicationsDbContext dbContext, ILogger<GetApplicationByIdQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<JobApplication?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Applications] Fetching application {Id}", request.ApplicationId);
        return await _dbContext.JobApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);
    }
}
