using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
}
