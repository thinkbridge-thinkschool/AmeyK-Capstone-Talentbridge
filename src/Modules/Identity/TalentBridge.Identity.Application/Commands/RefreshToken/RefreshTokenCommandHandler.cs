using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TalentBridge.Identity.Application.Interfaces;
using TalentBridge.Shared.Common;

namespace TalentBridge.Identity.Application.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResult>>
{
    private readonly IIdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(IIdentityDbContext dbContext, ITokenService tokenService, ILogger<RefreshTokenCommandHandler> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user is null || !user.IsRefreshTokenValid(request.RefreshToken))
        {
            _logger.LogWarning("[Identity] Invalid or expired refresh token used.");
            return Result<RefreshTokenResult>.Failure("Invalid or expired refresh token.");
        }

        var newAccessToken = _tokenService.GenerateToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(8);

        user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Identity] Refresh token rotated for user {UserId}", user.Id);

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult(newAccessToken, newRefreshToken, expiresAt));
    }
}
