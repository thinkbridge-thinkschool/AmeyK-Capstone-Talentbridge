using MediatR;

namespace TalentBridge.Jobs.Application.Commands.DeleteJob;

public record DeleteJobCommand(Guid JobId, Guid RequestedByHRId) : IRequest<Unit>;
