using TalentBridge.Identity.Domain.Enums;
using TalentBridge.Identity.Domain.Events;
using TalentBridge.Shared.Common;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Identity.Domain.Entities;

public class User : AggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; private set; }

    // Profile fields
    public string? Phone { get; private set; }
    public string? Title { get; private set; }
    public string? Bio { get; private set; }
    public string? Skills { get; private set; }
    public string? ResumeUrl { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public string? GitHubUrl { get; private set; }

    private User() { }

    public static Result<User> Create(string email, string passwordHash, UserRole role, string fullName = "")
    {
        if (string.IsNullOrWhiteSpace(email)) return Result<User>.Failure("Email is required.");
        if (!email.Contains('@')) return Result<User>.Failure("Email format is invalid.");
        if (string.IsNullOrWhiteSpace(passwordHash)) return Result<User>.Failure("Password hash is required.");

        var user = new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            FullName = fullName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.Role, DateTime.UtcNow));
        return Result<User>.Success(user);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void UpdateProfile(
        string? fullName, string? phone, string? title,
        string? bio, string? skills, string? resumeUrl,
        string? linkedInUrl, string? gitHubUrl)
    {
        if (!string.IsNullOrWhiteSpace(fullName)) FullName = fullName;
        Phone = phone;
        Title = title;
        Bio = bio;
        Skills = skills;
        ResumeUrl = resumeUrl;
        LinkedInUrl = linkedInUrl;
        GitHubUrl = gitHubUrl;
    }

    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAtUtc = expiresAt;
    }

    public bool IsRefreshTokenValid(string token) =>
        RefreshToken == token && RefreshTokenExpiresAtUtc > DateTime.UtcNow;

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAtUtc = null;
    }
}
