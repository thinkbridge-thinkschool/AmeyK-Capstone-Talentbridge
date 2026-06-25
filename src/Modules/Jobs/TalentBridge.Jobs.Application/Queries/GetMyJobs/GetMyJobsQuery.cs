using MediatR;
using TalentBridge.Jobs.Application.DTOs;

namespace TalentBridge.Jobs.Application.Queries.GetMyJobs;

public record GetMyJobsQuery(Guid HRId) : IRequest<List<JobDto>>;
