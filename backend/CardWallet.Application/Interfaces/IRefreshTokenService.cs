using CardWallet.Domain.Entities;

namespace CardWallet.Application.Interfaces;

public interface IRefreshTokenService
{
    RefreshToken Create(Guid userId);
}