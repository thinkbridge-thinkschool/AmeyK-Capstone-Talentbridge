using TalentBridge.Identity.Domain.Entities;

namespace TalentBridge.Identity.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
