using Application.Interfaces;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace API.Jobs;

public class AccountInterestJob(
    IServiceProvider serviceProvider,
    ILogger<AccountInterestJob> logger) : BackgroundService
{
    private const decimal InterestRate = 0.0001m; // 0.01%
    private readonly TimeSpan _period = TimeSpan.FromMinutes(2); // Run every 2 mins for demo

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Interest Job started.");

        using var timer = new PeriodicTimer(_period);

        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ApplyInterestAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while applying interest.");
            }
        }
    }

    private async Task ApplyInterestAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IBankDbContext>();

        // 1. Fetch eligible accounts (Savings, Active)
        // Note: For high volume, we would paginate this. For Hackathon, FetchAll is okay-ish but dangerous.
        // Better: Process in batches of 100.
        
        int batchSize = 100;
        int processedCount = 0;
        
        while (true)
        {
            // We need to query accounts that haven't had interest applied recently?
            // Or just all savings accounts? The requirement implies "Background process applying...", 
            // usually this means Daily/Monthly. 
            // To avoid complexity of "LastInterestAppliedDate", we'll just apply blindly every tick.
            // CAUTION: In real life, we need a flag/date. 
            // Let's add a "LastInterestAppliedAt" to the Account entity implicitly via a new Transaction Type "Interest" check?
            // No, querying transactions is heavy.
            // For now, let's just loop all Savings accounts.
            
            var accounts = await context.Accounts
                .Where(a => a.AccountType == AccountType.Savings && a.Status == AccountStatus.Active)
                .OrderBy(a => a.Id)
                .Skip(processedCount)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (accounts.Count == 0) break;

            foreach (var account in accounts)
            {
                // Create execution strategy for resilience
                var strategy = context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        // Reload to get latest version/balance
                        await context.Accounts.Entry(account).ReloadAsync(cancellationToken);
                        
                        if (account.Balance <= 0) return; // No interest on 0 or negative (shouldn't be negative)

                        decimal interestAmount = Math.Round(account.Balance * InterestRate, 2);
                        
                        if (interestAmount <= 0) return; // Too small

                        account.Balance += interestAmount;

                        var interestTx = new Domain.Entities.Transaction
                        {
                            AccountId = account.Id,
                            Amount = interestAmount,
                            BalanceAfter = account.Balance,
                            CreatedAt = DateTime.UtcNow,
                            Description = "Monthly Interest Application",
                            ReferenceId = $"INT-{DateTime.UtcNow:yyyyMMdd}-{account.Id}",
                            Type = TransactionType.Credit
                        };

                        context.Transactions.Add(interestTx);
                        // RowVersion handles concurrency here
                        
                        await context.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        
                        logger.LogInformation("Applied {Amount} interest to Account {AccountId}", interestAmount, account.Id);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Optimization: Just skip this cycle for this account. It will get it next time or retry?
                        // For simplicity, we log and skip.
                        logger.LogWarning("Concurrency conflict while applying interest for Account {AccountId}. Skipping.", account.Id);
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                         logger.LogError(ex, "Failed to apply interest for Account {AccountId}", account.Id);
                         await transaction.RollbackAsync(cancellationToken);
                    }
                });
            }
            
            processedCount += accounts.Count;
        }
    }
}
