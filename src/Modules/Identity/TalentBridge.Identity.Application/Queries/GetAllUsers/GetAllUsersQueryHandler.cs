using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Identity.Application.Interfaces;
using TalentBridge.Identity.Application.Queries.GetCurrentUser;

namespace TalentBridge.Identity.Application.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(IIdentityDbContext dbContext)
    : IRequestHandler<GetAllUsersQuery, List<CurrentUserDto>>
{
    public async Task<List<CurrentUserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => new CurrentUserDto(u.Id, u.Email, u.FullName, u.Role.ToString(), u.IsActive, u.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
