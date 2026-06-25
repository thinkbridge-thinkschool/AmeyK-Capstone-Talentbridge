using MediatR;
using TalentBridge.Shared.Common;

namespace TalentBridge.Identity.Application.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string? FullName,
    string? Phone,
    string? Title,
    string? Bio,
    string? Skills,
    string? ResumeUrl,
    string? LinkedInUrl,
    string? GitHubUrl) : IRequest<Result>;
