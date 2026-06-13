using System;
using System.Threading.Tasks;
using CardWallet.Application.DTOs.Wallet;
using CardWallet.Application.DTOs.CardExchange;
using CardWallet.Application.Interfaces;
using CardWallet.Application.Services;
using CardWallet.Domain.Entities;
using CardWallet.Domain.Enums;
using Moq;
using Xunit;

namespace CardWallet.Application.Tests;

public class CardExchangeServiceTests
{
    [Fact]
    public async Task SubmitCardAsync_ReturnsSuccess_WhenParentSucceeds()
    {
        var txRepoMock = new Mock<ICardTransactionRepository>();
        txRepoMock.Setup(x => x.AddAsync(It.IsAny<CardTransaction>())).Returns(Task.CompletedTask);
        txRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var rateRepoMock = new Mock<ICardRateRepository>();
        rateRepoMock.Setup(x => x.GetByProviderAndFaceValueAsync(CardProvider.Viettel, 500000))
            .ReturnsAsync(new CardRate { Provider = CardProvider.Viettel, FaceValue = 500000, DiscountPercent = 20m, IsActive = true });

        var parentMock = new Mock<IParentCardApiClient>();
        var walletMock = new Mock<IWalletService>();

        var service = new CardExchangeService(txRepoMock.Object, rateRepoMock.Object, parentMock.Object, walletMock.Object);

        var result = await service.SubmitCardAsync(Guid.NewGuid(), new SubmitCardRequest { Provider = "Viettel", FaceValue = 500000, CardCode = "CODE", Serial = "SER" });

        // Should create pending transaction and not call parent or wallet
        txRepoMock.Verify(x => x.AddAsync(It.IsAny<CardTransaction>()), Times.Once);
        parentMock.Verify(x => x.SubmitCardAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        walletMock.Verify(x => x.AdjustBalanceAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<Domain.Enums.WalletTransactionType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task SubmitCardAsync_ReturnsExisting_WhenIdempotencyKeyMatches()
    {
        var existing = new CardTransaction { Id = Guid.NewGuid(), Status = CardTransactionStatus.Success, ActualReceiveAmount = 1000, CreatedAt = DateTime.UtcNow };

        var txRepoMock = new Mock<ICardTransactionRepository>();
        txRepoMock.Setup(x => x.GetByIdempotencyKeyAsync("idem-1")).ReturnsAsync(existing);

        var service = new CardExchangeService(txRepoMock.Object, Mock.Of<ICardRateRepository>(), Mock.Of<IParentCardApiClient>(), Mock.Of<IWalletService>());

        var result = await service.SubmitCardAsync(Guid.NewGuid(), new SubmitCardRequest { Provider = "Viettel", FaceValue = 1000, CardCode = "C", Serial = "S", IdempotencyKey = "idem-1" });

        Assert.Equal(existing.ActualReceiveAmount, result.ActualReceiveAmount);
        Assert.Equal(existing.Status.ToString(), result.Status);
    }

    [Fact]
    public async Task FinalizeTransactionAsync_CreditsWallet_WhenParentReturnsSuccess()
    {
        var tx = new CardTransaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = CardTransactionStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        var txRepoMock = new Mock<ICardTransactionRepository>();
        txRepoMock.Setup(x => x.GetByIdAsync(tx.Id)).ReturnsAsync(tx);
        txRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var walletMock = new Mock<IWalletService>();
        walletMock.Setup(x => x.AdjustBalanceAsync(tx.UserId, 400, WalletTransactionType.CardExchange, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new WalletTransactionDto());

        var service = new CardExchangeService(txRepoMock.Object, Mock.Of<ICardRateRepository>(), Mock.Of<IParentCardApiClient>(), walletMock.Object);

        var result = await service.FinalizeTransactionAsync(tx.Id, new ParentCardApiResponse
        {
            Success = true,
            ActualReceiveAmount = 400000,
            ParentTransactionCode = "PARENT-123"
        });

        walletMock.Verify(x => x.AdjustBalanceAsync(tx.UserId, 400, WalletTransactionType.CardExchange, It.Is<string>(s => s.Contains("finalize")), It.IsAny<string>(), $"CARD-{tx.Id}"), Times.Once);
        Assert.Equal(CardTransactionStatus.Success.ToString(), result.Status);
        Assert.Equal(400000, result.ActualReceiveAmount);
        Assert.Equal("PARENT-123", tx.ParentTransactionCode);
    }
}
