using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Data;

namespace Application.Services;

public class TransferService(IBankDbContext context, ILogger<TransferService> logger) : ITransferService
{
    private readonly ILogger<TransferService> _logger = logger;
    public async Task<ServiceResult<AccountResponseDto>> TransferAsync(TransferDto request, int userId)
    {
        var fromAccount = await context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == request.FromAccountNumber);

        if (fromAccount == null)
            return ServiceResult<AccountResponseDto>.Failure("Source account not found.", 404);

        if (fromAccount.UserId != userId)
        {
            return ServiceResult<AccountResponseDto>.Failure("Unauthorized access to source account.", 403);
        }
        if (fromAccount.Status != AccountStatus.Active)
        {
            return ServiceResult<AccountResponseDto>.Failure("Source account is not active.", 400);
        }

        if (fromAccount.Balance < request.Amount)
        {
            return ServiceResult<AccountResponseDto>.Failure("Insufficient funds.", 400);
        }

        var toAccount = await context.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccountNumber);

        if (toAccount == null)
        {
            return ServiceResult<AccountResponseDto>.Failure("Destination account not found.", 404);
        }

        if (toAccount.Status != AccountStatus.Active)
        {
            return ServiceResult<AccountResponseDto>.Failure("Destination account is not active.", 400);
        }

        if (fromAccount.Id == toAccount.Id)
        {
            return ServiceResult<AccountResponseDto>.Failure("Cannot transfer to the same account.", 400);
        }

        var retryPolicy = CreateRetryPolicy(_logger);

        return await retryPolicy.ExecuteAsync(async () =>
        {

            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                fromAccount.Balance -= request.Amount;

                toAccount.Balance += request.Amount;

                var referenceId = Guid.NewGuid().ToString();

                var debitTransaction = new Transaction
                {
                    Type = TransactionType.Transfer,
                    Amount = request.Amount,
                    AccountId = fromAccount.Id,
                    RelatedAccountId = toAccount.Id,
                    BalanceAfter = fromAccount.Balance,
                    ReferenceId = referenceId,
                    Description = string.IsNullOrEmpty(request.Description)
                        ? $"Transfer to {toAccount.AccountNumber}"
                        : $"Transfer to {toAccount.AccountNumber}: {request.Description}"
                };

                var creditTransaction = new Transaction
                {
                    Type = TransactionType.Transfer,
                    Amount = request.Amount,
                    AccountId = toAccount.Id,
                    RelatedAccountId = fromAccount.Id,
                    BalanceAfter = toAccount.Balance,
                    ReferenceId = referenceId,
                    Description = string.IsNullOrEmpty(request.Description)
                        ? $"Transfer from {fromAccount.AccountNumber}"
                        : $"Transfer from {fromAccount.AccountNumber}: {request.Description}"
                };

                context.Transactions.Add(debitTransaction);
                context.Transactions.Add(creditTransaction);

                context.Accounts.Update(fromAccount);
                context.Accounts.Update(toAccount);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult<AccountResponseDto>.SuccessResult(
                    fromAccount.Adapt<AccountResponseDto>(),
                    "Transfer successful.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                throw;
                return ServiceResult<AccountResponseDto>.Failure("Concurrency conflict during transfer. Please try again.", 409);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transfer failed");
                return ServiceResult<AccountResponseDto>.Failure($"Transfer failed: {ex.Message}", 500);
            }
        });
    }
        private static AsyncRetryPolicy CreateRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<DbUpdateConcurrencyException>()
            .Or<DbUpdateException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(200 * retryAttempt),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "Retry {RetryCount} after {Delay}ms due to concurrency issue",
                        retryCount,
                        timeSpan.TotalMilliseconds
                    );
                });
    }
}
