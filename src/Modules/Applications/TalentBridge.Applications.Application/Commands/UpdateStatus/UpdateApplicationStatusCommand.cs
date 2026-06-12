using MediatR;

namespace TalentBridge.Applications.Application.Commands.UpdateStatus;

public record UpdateApplicationStatusCommand(
    Guid ApplicationId,
    string NewStatus,
    string? RejectionReason = null) : IRequest<Unit>;
