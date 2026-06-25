using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Applications.Application.Interfaces;

namespace TalentBridge.Applications.Application.Queries.GetApplicationHistory;

public class GetApplicationHistoryQueryHandler(IApplicationsDbContext dbContext)
    : IRequestHandler<GetApplicationHistoryQuery, List<ApplicationHistoryDto>>
{
    public async Task<List<ApplicationHistoryDto>> Handle(
        GetApplicationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.StatusHistory
            .Where(h => h.ApplicationId == request.ApplicationId)
            .OrderBy(h => h.ChangedAtUtc)
            .Select(h => new ApplicationHistoryDto(
                h.Id,
                h.FromStatus,
                h.ToStatus,
                h.ChangedByUserId,
                h.Notes,
                h.ChangedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
