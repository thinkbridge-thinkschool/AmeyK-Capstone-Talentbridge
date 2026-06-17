using MediatR;
using TalentBridge.Jobs.Application.DTOs;

namespace TalentBridge.Jobs.Application.Queries.SearchJobs;

public record SearchJobsQuery(
    string? Keyword,
    string? Location,
    int Page,
    int Size) : IRequest<List<JobDto>>;
