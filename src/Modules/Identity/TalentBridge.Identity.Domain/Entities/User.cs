using TalentBridge.Identity.Domain.Enums;
using TalentBridge.Shared.Domain;

namespace TalentBridge.Identity.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public Guid? CompanyId { get; private set; }

    private User() { }

    public static User Create(string email, string password, UserRole role, string firstName, string lastName, Guid? companyId = null)
    {
        if (!email.Contains('@') || !email.Contains('.'))
            throw new ArgumentException("Email must contain @ and .", nameof(email));

        if (password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.", nameof(password));

        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            FirstName = firstName,
            LastName = lastName,
            CompanyId = companyId,
            IsActive = true
        };
    }

    public bool VerifyPassword(string plainPassword) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, PasswordHash);

    public void Deactivate() => IsActive = false;

    public void UpdateLastLogin() => LastLoginAt = DateTime.UtcNow;
}
