using TalentBridge.Identity.Domain.Entities;
using TalentBridge.Identity.Domain.Enums;
using TalentBridge.Identity.Domain.Events;

namespace TalentBridge.Identity.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("securePass1");
        var result = User.Create("test@example.com", passwordHash, UserRole.Candidate);

        Assert.True(result.IsSuccess);
        var user = result.Value!;
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal(UserRole.Candidate, user.Role);
        Assert.NotEqual("securePass1", user.PasswordHash);
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldFail()
    {
        var result = User.Create("notanemail", "someHash", UserRole.Candidate);
        Assert.True(result.IsFailure);
        Assert.Contains("Email", result.Error);
    }

    [Fact]
    public void Create_WithEmptyEmail_ShouldFail()
    {
        var result = User.Create("", "someHash", UserRole.Candidate);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_ShouldFail()
    {
        var result = User.Create("test@example.com", "", UserRole.Candidate);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_ShouldRaiseUserRegisteredEvent()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("securePass1");
        var result = User.Create("test@example.com", passwordHash, UserRole.Candidate);

        Assert.True(result.IsSuccess);
        var user = result.Value!;
        var evt = user.DomainEvents.OfType<UserRegisteredEvent>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(user.Id, evt.UserId);
        Assert.Equal(user.Email, evt.Email);
    }

    [Fact]
    public void SetRefreshToken_ShouldBeValid()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("securePass1");
        var user = User.Create("test@example.com", passwordHash, UserRole.Candidate).Value!;
        var token = "my-refresh-token";
        var expiry = DateTime.UtcNow.AddDays(7);

        user.SetRefreshToken(token, expiry);

        Assert.True(user.IsRefreshTokenValid(token));
    }

    [Fact]
    public void RevokeRefreshToken_ShouldInvalidateToken()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("securePass1");
        var user = User.Create("test@example.com", passwordHash, UserRole.Candidate).Value!;
        user.SetRefreshToken("my-token", DateTime.UtcNow.AddDays(7));
        user.RevokeRefreshToken();

        Assert.False(user.IsRefreshTokenValid("my-token"));
    }
}
