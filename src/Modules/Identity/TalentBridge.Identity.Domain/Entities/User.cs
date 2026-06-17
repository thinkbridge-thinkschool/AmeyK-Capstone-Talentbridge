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
    public DateTime CreatedAtUtc { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; private set; }

    private User() { }

    public static Result<User> Create(string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email)) return Result<User>.Failure("Email is required.");
        if (!email.Contains('@')) return Result<User>.Failure("Email format is invalid.");
        if (string.IsNullOrWhiteSpace(passwordHash)) return Result<User>.Failure("Password hash is required.");

        var user = new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.Role, DateTime.UtcNow));
        return Result<User>.Success(user);
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
