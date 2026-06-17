using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TalentBridge.Identity.Application.Interfaces;
using TalentBridge.Shared.Common;

namespace TalentBridge.Identity.Application.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private readonly IIdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(IIdentityDbContext dbContext, ITokenService tokenService, ILogger<LoginCommandHandler> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("[Identity] Failed login attempt for {Email}", request.Email);
            return Result<LoginResult>.Failure("Invalid credentials");
        }

        var token = _tokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        _logger.LogInformation("[Identity] User {UserId} logged in", user.Id);

        return Result<LoginResult>.Success(new LoginResult(token, expiresAt, user.Role.ToString()));
    }
}
