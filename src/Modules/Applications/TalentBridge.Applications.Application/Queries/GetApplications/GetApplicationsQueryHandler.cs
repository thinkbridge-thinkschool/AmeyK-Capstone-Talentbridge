using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Applications.Application.Interfaces;

namespace TalentBridge.Applications.Application.Queries.GetApplications;

public class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, List<ApplicationSummaryDto>>
{
    private readonly IApplicationsDbContext _dbContext;

    public GetApplicationsQueryHandler(IApplicationsDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<ApplicationSummaryDto>> Handle(GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.JobApplications.AsNoTracking();

        if (request.CandidateId.HasValue)
            query = query.Where(a => a.CandidateId == request.CandidateId.Value);

        if (request.JobId.HasValue)
            query = query.Where(a => a.JobId == request.JobId.Value);

        return await query
            .OrderByDescending(a => a.SubmittedAtUtc)
            .Select(a => new ApplicationSummaryDto(
                a.Id,
                a.CandidateId,
                a.JobId,
                a.Status.ToString(),
                a.CoverLetter,
                a.ResumeUrl,
                a.SubmittedAtUtc,
                a.LastUpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
