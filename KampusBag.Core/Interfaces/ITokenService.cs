using KampusBag.Core.Entities;

namespace KampusBag.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
