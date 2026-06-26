using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TalentBridge.Applications.Application.DTOs;
using TalentBridge.Applications.Application.Interfaces;

namespace TalentBridge.Applications.Application.Queries.GetApplication;

public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, ApplicationDetailDto?>
{
    private readonly IApplicationsDbContext _dbContext;
    private readonly ILogger<GetApplicationByIdQueryHandler> _logger;

    public GetApplicationByIdQueryHandler(IApplicationsDbContext dbContext, ILogger<GetApplicationByIdQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApplicationDetailDto?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Applications] Fetching application {Id}", request.ApplicationId);

        return await _dbContext.JobApplications
            .AsNoTracking()
            .Where(a => a.Id == request.ApplicationId)
            .Select(a => new ApplicationDetailDto(
                a.Id,
                a.CandidateId,
                a.JobId,
                a.Status.ToString(),
                a.CoverLetter,
                a.ResumeUrl,
                a.SubmittedAtUtc,
                a.LastUpdatedAtUtc,
                a.ReviewNotes,
                a.MatchPercentage,
                a.ReviewedByHRId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
