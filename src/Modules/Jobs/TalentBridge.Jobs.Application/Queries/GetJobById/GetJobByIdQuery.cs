using MediatR;
using TalentBridge.Jobs.Application.DTOs;

namespace TalentBridge.Jobs.Application.Queries.GetJobById;

public record GetJobByIdQuery(Guid JobId) : IRequest<JobDto?>;
