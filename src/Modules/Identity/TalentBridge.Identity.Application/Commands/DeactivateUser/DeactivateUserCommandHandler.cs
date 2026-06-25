using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Identity.Application.Interfaces;
using TalentBridge.Shared.Common;

namespace TalentBridge.Identity.Application.Commands.DeactivateUser;

public class DeactivateUserCommandHandler(IIdentityDbContext dbContext)
    : IRequestHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null) return Result.Failure("User not found.");

        user.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
