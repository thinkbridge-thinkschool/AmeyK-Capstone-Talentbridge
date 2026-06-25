using MediatR;

namespace TalentBridge.Jobs.Application.Commands.CloseJob;

public record CloseJobCommand(Guid JobId, Guid RequestingHRId) : IRequest<Unit>;
