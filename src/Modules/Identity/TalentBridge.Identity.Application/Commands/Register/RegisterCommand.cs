using MediatR;

namespace TalentBridge.Identity.Application.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string Role) : IRequest<RegisterResult>;

public record RegisterResult(Guid UserId, string Email, string Role);
