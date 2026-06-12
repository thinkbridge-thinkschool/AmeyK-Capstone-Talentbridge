using MediatR;
using TalentBridge.Shared.Common;

namespace TalentBridge.Identity.Application.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResult>>;

public record LoginResult(string Token, DateTime ExpiresAt, string UserRole);
