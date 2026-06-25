using MediatR;

namespace TalentBridge.Identity.Application.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserDto?>;

public record CurrentUserDto(
    Guid Id, string Email, string FullName, string Role, bool IsActive, DateTime CreatedAtUtc,
    string? Phone = null, string? Title = null, string? Bio = null,
    string? Skills = null, string? ResumeUrl = null, string? LinkedInUrl = null, string? GitHubUrl = null);
