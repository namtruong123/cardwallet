using System.Security.Cryptography;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Entities;

namespace CardWallet.Infrastructure.Security;

public class RefreshTokenService : IRefreshTokenService
{
    public RefreshToken Create(Guid userId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
    }
}