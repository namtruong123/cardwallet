using System.Text.Json;
using CardWallet.Application.DTOs.CardExchange;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CardWallet.Api.Workers;

public class CardExchangeWorker : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<CardExchangeWorker> _logger;

    public CardExchangeWorker(IServiceProvider provider, ILogger<CardExchangeWorker> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CardExchangeWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var txRepo = scope.ServiceProvider.GetRequiredService<CardWallet.Application.Interfaces.ICardTransactionRepository>();
                var exchangeService = scope.ServiceProvider.GetRequiredService<CardWallet.Application.Interfaces.ICardExchangeService>();
                var parent = scope.ServiceProvider.GetRequiredService<CardWallet.Application.Interfaces.IParentCardApiClient>();
                var db = scope.ServiceProvider.GetRequiredService<CardWallet.Infrastructure.Data.AppDbContext>();

                var toProcess = await txRepo.GetPendingAsync(20);
                foreach (var tx in toProcess)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        _logger.LogInformation("Processing card tx {Id}", tx.Id);

                        tx.Status = CardTransactionStatus.Processing;
                        tx.ProcessedAt = DateTime.UtcNow;
                        tx.RetryCount += 1;
                        tx.NextRetryAt = DateTime.UtcNow.AddSeconds(30 * tx.RetryCount);
                        await txRepo.SaveChangesAsync();

                        var parentResult = await parent.SubmitCardAsync(tx.Provider.ToString(), tx.FaceValue, tx.CardCode, tx.Serial, tx.IdempotencyKey ?? string.Empty);

                        tx.ParentRequestRaw = JsonSerializer.Serialize(new { tx.Id, tx.CardCode, tx.Serial, tx.FaceValue });
                        tx.ParentResponseRaw = JsonSerializer.Serialize(parentResult);

                        if (parentResult.Success)
                        {
                            await using var tran = await db.Database.BeginTransactionAsync();
                            try
                            {
                                await exchangeService.FinalizeTransactionAsync(tx.Id, parentResult);
                                await tran.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error committing transaction {Id}", tx.Id);
                                await tran.RollbackAsync();

                                tx.Status = CardTransactionStatus.NeedReconcile;
                                tx.ErrorMessage = ex.Message;
                                await txRepo.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            tx.Status = CardTransactionStatus.Failed;
                            tx.FailureReason = parentResult.FailureReason;
                            tx.ErrorMessage = parentResult.FailureReason;
                            tx.CompletedAt = DateTime.UtcNow;
                            await txRepo.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Worker loop error for tx {Id}", tx.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CardExchangeWorker top-level error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
