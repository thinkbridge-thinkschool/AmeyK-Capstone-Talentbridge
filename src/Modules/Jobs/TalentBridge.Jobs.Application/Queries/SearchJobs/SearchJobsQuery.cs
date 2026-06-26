using MediatR;
using TalentBridge.Jobs.Application.DTOs;

namespace TalentBridge.Jobs.Application.Queries.SearchJobs;

public record SearchJobsQuery(
    string? Keyword,
    string? Location,
    int Page,
    int Size,
    decimal? SalaryMin = null,
    decimal? SalaryMax = null) : IRequest<PagedResult<JobDto>>;
