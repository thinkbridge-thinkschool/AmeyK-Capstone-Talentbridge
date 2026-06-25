using MediatR;
using TalentBridge.Shared.Common;

namespace TalentBridge.Identity.Application.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid UserId) : IRequest<Result>;
