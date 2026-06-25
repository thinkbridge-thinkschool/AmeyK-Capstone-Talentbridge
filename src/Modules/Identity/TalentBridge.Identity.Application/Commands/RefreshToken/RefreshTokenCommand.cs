using MediatR;
using TalentBridge.Shared.Common;

namespace TalentBridge.Identity.Application.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResult>>;

public record RefreshTokenResult(string AccessToken, string RefreshToken, DateTime ExpiresAt);
