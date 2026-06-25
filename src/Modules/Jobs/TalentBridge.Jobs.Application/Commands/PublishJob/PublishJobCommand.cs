using MediatR;

namespace TalentBridge.Jobs.Application.Commands.PublishJob;

public record PublishJobCommand(Guid JobId, Guid RequestingHRId) : IRequest<Unit>;
