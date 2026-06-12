using TalentBridge.Identity.Domain.Entities;
using TalentBridge.Identity.Domain.Enums;

namespace TalentBridge.Identity.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Create_WithValidPassword_ShouldHashPassword()
    {
        var user = User.Create("test@example.com", "securePass1", UserRole.Candidate, "Test", "User");
        Assert.NotEqual("securePass1", user.PasswordHash);
        Assert.False(string.IsNullOrEmpty(user.PasswordHash));
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var user = User.Create("test@example.com", "securePass1", UserRole.Candidate, "Test", "User");
        Assert.True(user.VerifyPassword("securePass1"));
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var user = User.Create("test@example.com", "securePass1", UserRole.Candidate, "Test", "User");
        Assert.False(user.VerifyPassword("wrongPassword"));
    }

    [Fact]
    public void Create_WithShortPassword_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            User.Create("test@example.com", "short", UserRole.Candidate, "Test", "User"));
        Assert.Contains("Password", ex.Message);
    }
}
