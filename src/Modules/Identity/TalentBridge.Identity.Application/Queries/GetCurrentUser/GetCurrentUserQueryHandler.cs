using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Identity.Application.Interfaces;

namespace TalentBridge.Identity.Application.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto?>
{
    private readonly IIdentityDbContext _dbContext;

    public GetCurrentUserQueryHandler(IIdentityDbContext dbContext) => _dbContext = dbContext;

    public async Task<CurrentUserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null) return null;

        return new CurrentUserDto(
            user.Id, user.Email, user.FullName, user.Role.ToString(), user.IsActive, user.CreatedAtUtc,
            user.Phone, user.Title, user.Bio, user.Skills, user.ResumeUrl, user.LinkedInUrl, user.GitHubUrl);
    }
}
