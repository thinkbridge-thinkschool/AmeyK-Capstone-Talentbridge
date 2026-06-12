using MediatR;
using Microsoft.Extensions.Logging;
using TalentBridge.Identity.Application.Interfaces;
using TalentBridge.Identity.Domain.Entities;
using TalentBridge.Identity.Domain.Enums;

namespace TalentBridge.Identity.Application.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IIdentityDbContext _dbContext;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(IIdentityDbContext dbContext, ILogger<RegisterCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new ArgumentException($"Invalid role: {request.Role}");

        var user = User.Create(request.Email, request.Password, role, request.FirstName, request.LastName, request.CompanyId);

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Identity] User {UserId} registered with role {Role}", user.Id, role);

        return new RegisterResult(user.Id, user.Email, user.Role.ToString());
    }
}
