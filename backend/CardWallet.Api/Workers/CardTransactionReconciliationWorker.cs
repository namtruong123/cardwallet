using System.Text.Json;
using CardWallet.Application.DTOs.CardExchange;
using CardWallet.Application.Interfaces;
using CardWallet.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CardWallet.Api.Workers;

public class CardTransactionReconciliationWorker : BackgroundService
{
    private readonly ILogger<CardTransactionReconciliationWorker> _logger;
    private readonly IServiceProvider _provider;

    public CardTransactionReconciliationWorker(IServiceProvider provider, ILogger<CardTransactionReconciliationWorker> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CardTransactionReconciliationWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var txRepo = scope.ServiceProvider.GetRequiredService<CardWallet.Application.Interfaces.ICardTransactionRepository>();
                var exchangeService = scope.ServiceProvider.GetRequiredService<CardWallet.Application.Interfaces.ICardExchangeService>();
                var parent = scope.ServiceProvider.GetRequiredService<CardWallet.Application.Interfaces.IParentCardApiClient>();
                var db = scope.ServiceProvider.GetRequiredService<CardWallet.Infrastructure.Data.AppDbContext>();

                var toCheck = await txRepo.GetPendingAsync(50);
                foreach (var tx in toCheck)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    if (string.IsNullOrWhiteSpace(tx.ParentTransactionCode))
                        continue;

                    try
                    {
                        _logger.LogInformation("Reconciling card tx {Id} parent {Parent}", tx.Id, tx.ParentTransactionCode);

                        var parentResult = await parent.GetTransactionStatusAsync(tx.ParentTransactionCode!);

                        tx.ParentResponseRaw = JsonSerializer.Serialize(parentResult);
                        tx.ProcessedAt = DateTime.UtcNow;

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
                                _logger.LogError(ex, "Error committing reconciliation for {Id}", tx.Id);
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
                        _logger.LogError(ex, "Reconciliation loop error for tx {Id}", tx.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CardTransactionReconciliationWorker top-level error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
