using MediatR;
using TalentBridge.Jobs.Application.DTOs;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Application.Queries.GetAllJobsAdmin;

public class GetAllJobsAdminQueryHandler(IJobRepository repository)
    : IRequestHandler<GetAllJobsAdminQuery, List<JobDto>>
{
    public async Task<List<JobDto>> Handle(GetAllJobsAdminQuery request, CancellationToken cancellationToken)
    {
        var jobs = await repository.GetAllAsync(cancellationToken);
        return jobs.Select(j => new JobDto(
            j.Id, j.CompanyId, j.PostedByHRId, j.Title, j.Description,
            j.Location, j.SalaryMin, j.SalaryMax, j.Status,
            j.CreatedAtUtc, j.PublishedAtUtc, j.ExpiresAtUtc)).ToList();
    }
}
