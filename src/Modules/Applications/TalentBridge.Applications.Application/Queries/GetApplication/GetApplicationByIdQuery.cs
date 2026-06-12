using MediatR;
using TalentBridge.Applications.Domain.Aggregates;

namespace TalentBridge.Applications.Application.Queries.GetApplication;

public record GetApplicationByIdQuery(Guid ApplicationId) : IRequest<JobApplication?>;
