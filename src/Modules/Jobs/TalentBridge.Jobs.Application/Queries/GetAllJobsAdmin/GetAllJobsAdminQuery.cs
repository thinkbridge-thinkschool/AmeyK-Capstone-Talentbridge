using MediatR;
using TalentBridge.Jobs.Application.DTOs;

namespace TalentBridge.Jobs.Application.Queries.GetAllJobsAdmin;

public record GetAllJobsAdminQuery : IRequest<List<JobDto>>;
