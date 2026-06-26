using MediatR;
using TalentBridge.Applications.Application.Services;

namespace TalentBridge.Applications.Application.Queries.GetApplicationMatch;

public record GetApplicationMatchQuery(Guid ApplicationId) : IRequest<MatchResult?>;
