using MediatR;

namespace TalentBridge.Identity.Application.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string Role,
    string FullName = "") : IRequest<RegisterResult>;

public record RegisterResult(Guid UserId, string Email, string Role, string FullName);
