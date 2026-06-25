using MediatR;
using Microsoft.EntityFrameworkCore;
using TalentBridge.Identity.Application.Interfaces;
using TalentBridge.Shared.Common;

namespace TalentBridge.Identity.Application.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(IIdentityDbContext dbContext)
    : IRequestHandler<UpdateProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null) return Result.Failure("User not found.");

        user.UpdateProfile(
            request.FullName,
            request.Phone,
            request.Title,
            request.Bio,
            request.Skills,
            request.ResumeUrl,
            request.LinkedInUrl,
            request.GitHubUrl);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
