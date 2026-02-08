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

    private readonly AsyncRetryPolicy<ServiceResult<AccountResponseDto>> _retryPolicy = CreateRetryPolicy(logger);

    public async Task<ServiceResult<string>> InitiateTransferAsync(string fromAccountNumber, string toAccountNumber, int userId)
    {
        var fromAccount = await context.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == fromAccountNumber);

        
        if (fromAccount == null)
            return ServiceResult<string>.Failure("Source account not found.", 404);

        if (fromAccount.UserId != userId)
            return ServiceResult<string>.Failure("Unauthorized access to source account.", 403);

        if (fromAccount.Status != AccountStatus.Active)
            return ServiceResult<string>.Failure("Source account is not active.", 400);

        var toAccount = await context.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == toAccountNumber);

        if (toAccount == null)
            return ServiceResult<string>.Failure("Destination account not found.", 404);

        if (toAccount.Status != AccountStatus.Active)
            return ServiceResult<string>.Failure("Destination account is not active.", 400);

        if (fromAccount.Id == toAccount.Id)
            return ServiceResult<string>.Failure("Cannot transfer to the same account.", 400);

        var transferIntent = new TransferIntents
        {
            TransferIntentId = Guid.NewGuid().ToString(),
            FromAccountId = fromAccount.Id,
            ToAccountId = toAccount.Id,
            Status = TransferIntentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.TransferIntents.Add(transferIntent);
        await context.SaveChangesAsync();

        return ServiceResult<string>.SuccessResult(transferIntent.TransferIntentId, "Transfer intent created. Use the idempotency key to complete the transfer.");
    }

    public async Task<ServiceResult<AccountResponseDto>> CompleteTransferAsync(string intentId, decimal amount, string description, int userId)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var transferIntent = await context.TransferIntents
                .Include(x => x.FromAccount)
                .Include(x => x.ToAccount)
                .FirstOrDefaultAsync(x => x.TransferIntentId == intentId);

            if (transferIntent == null)
                return ServiceResult<AccountResponseDto>.Failure("Transfer intent not found.", 404);

            var transferIntentValidationResult = ValidateTransferIntent(transferIntent, userId);
            if (!transferIntentValidationResult.Success)
                return transferIntentValidationResult;

            var fromAccount = transferIntent.FromAccount;
            var toAccount = transferIntent.ToAccount;

            if (fromAccount == null)
                return ServiceResult<AccountResponseDto>.Failure("Source account not found.", 404);

            if (toAccount == null)
                return ServiceResult<AccountResponseDto>.Failure("Destination account not found.", 404);

            var transferValidationResult = ValidateTransfer(transferIntent, amount);
            if (!transferValidationResult.Success)
                return transferValidationResult;

            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                fromAccount.Balance -= amount;
                toAccount.Balance += amount;

                var referenceId = Guid.NewGuid().ToString();

                var debitTransaction = new Transaction
                {
                    Type = TransactionType.Transfer,
                    Amount = amount,
                    AccountId = fromAccount.Id,
                    RelatedAccountId = toAccount.Id,
                    BalanceAfter = fromAccount.Balance,
                    ReferenceId = referenceId,
                    Description = string.IsNullOrEmpty(description)
                        ? $"Transfer to {toAccount.AccountNumber}"
                        : $"Transfer to {toAccount.AccountNumber}: {description}"
                };

                var creditTransaction = new Transaction
                {
                    Type = TransactionType.Transfer,
                    Amount = amount,
                    AccountId = toAccount.Id,
                    RelatedAccountId = fromAccount.Id,
                    BalanceAfter = toAccount.Balance,
                    ReferenceId = referenceId,
                    Description = string.IsNullOrEmpty(description)
                        ? $"Transfer from {fromAccount.AccountNumber}"
                        : $"Transfer from {fromAccount.AccountNumber}: {description}"
                };

                context.Transactions.Add(debitTransaction);
                context.Transactions.Add(creditTransaction);

                context.Accounts.Update(fromAccount);
                context.Accounts.Update(toAccount);

                SetTransferIntentStatus(context, transferIntent, TransferIntentStatus.Completed);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult<AccountResponseDto>.SuccessResult(
                    fromAccount.Adapt<AccountResponseDto>(),
                    "Transfer successful.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("Transfer failed due to concurrency issue");
                throw new Exception("Transfer failed due to concurrency issue"); // Let the retry policy handle this
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transfer failed");
                SetTransferIntentStatus(context, transferIntent!, TransferIntentStatus.Failed);
                return ServiceResult<AccountResponseDto>.Failure($"Transfer failed: {ex.Message}", 500);
            }
        });
    }

    public async Task<ServiceResult<AccountResponseDto>> CancelTransferAsync(string intentId, int userId)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var transferIntent = await context.TransferIntents
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .FirstOrDefaultAsync(t => t.TransferIntentId == intentId);

            if (transferIntent == null)
                return ServiceResult<AccountResponseDto>.Failure("Transfer intent not found.", 404);

            var transferIntentValidationResult = ValidateTransferIntent(transferIntent, userId);
            if (!transferIntentValidationResult.Success)
                return transferIntentValidationResult;

            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                SetTransferIntentStatus(context, transferIntent, TransferIntentStatus.Cancelled);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (transferIntent.FromAccount == null)
                    return ServiceResult<AccountResponseDto>.Failure("Source account not found.", 404);

                return ServiceResult<AccountResponseDto>.SuccessResult(
                    transferIntent.FromAccount.Adapt<AccountResponseDto>(),
                    "Transfer cancelled successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("Transfer cancellation failed due to concurrency issue");
                throw new Exception("Transfer cancellation failed due to concurrency issue"); // Let the retry policy handle this
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transfer cancellation failed");
                return ServiceResult<AccountResponseDto>.Failure($"Transfer cancellation failed: {ex.Message}", 500);
            }
        });
    }

    private static AsyncRetryPolicy<ServiceResult<AccountResponseDto>> CreateRetryPolicy(ILogger logger)
    {
        return Policy<ServiceResult<AccountResponseDto>>
            .Handle<DbUpdateConcurrencyException>()
            .Or<DbUpdateException>()
            .OrResult(r => r?.StatusCode == 409)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(200 * retryAttempt),
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    if (outcome.Exception is Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Retry {RetryCount} after {Delay}ms due to concurrency issue",
                            retryCount,
                            timeSpan.TotalMilliseconds
                        );
                    }
                    else
                    {
                        logger.LogWarning(
                            "Retry {RetryCount} after {Delay}ms due to concurrency issue",
                            retryCount,
                            timeSpan.TotalMilliseconds
                        );
                    }
                });
    }

    private static void SetTransferIntentStatus(IBankDbContext context, TransferIntents transferIntent, TransferIntentStatus status)
    {
        transferIntent.Status = status;
        transferIntent.CompletedAt = DateTime.UtcNow;
        context.TransferIntents.Update(transferIntent);
    }

    private static ServiceResult<AccountResponseDto> ValidateTransferIntent(TransferIntents? transferIntent, int userId)
    {
        if (transferIntent == null)
            return ServiceResult<AccountResponseDto>.Failure("Transfer intent not found.", 404);

        if (transferIntent.Status != TransferIntentStatus.Pending)
            return ServiceResult<AccountResponseDto>.Failure("Transfer intent is not pending.", 400);

        if (transferIntent.FromAccount == null)
            return ServiceResult<AccountResponseDto>.Failure("Source account not found.", 404);

        if (transferIntent.FromAccount.UserId != userId)
            return ServiceResult<AccountResponseDto>.Failure("Unauthorized access to source account.", 403);

        if (transferIntent.ToAccount == null)
            return ServiceResult<AccountResponseDto>.Failure("Destination account not found.", 404);

        if (transferIntent.FromAccount.Status != AccountStatus.Active)
            return ServiceResult<AccountResponseDto>.Failure("Source account is not active.", 400);
        if (transferIntent.ToAccount.Status != AccountStatus.Active)
            return ServiceResult<AccountResponseDto>.Failure("Destination account is not active.", 400);
        if (transferIntent.FromAccount.Id == transferIntent.ToAccount.Id)
            return ServiceResult<AccountResponseDto>.Failure("Source and destination accounts cannot be the same.", 400);
        // if (transferIntent.FromAccount.Currency != transferIntent.ToAccount.Currency)
        //     return ServiceResult<AccountResponseDto>.Failure("Source and destination accounts must have the same currency.", 400); 
        // For Future Implementation of Money Data Type

        return ServiceResult<AccountResponseDto>.SuccessResult(transferIntent.FromAccount.Adapt<AccountResponseDto>(), "Transfer intent validated successfully.");
    }

    private static ServiceResult<AccountResponseDto> ValidateTransfer(TransferIntents transferIntent, decimal amount)
    {
        if (transferIntent.FromAccount == null)
            return ServiceResult<AccountResponseDto>.Failure("Source account not found.", 404);

        if (transferIntent.ToAccount == null)
            return ServiceResult<AccountResponseDto>.Failure("Destination account not found.", 404);

        if (transferIntent.FromAccount.Balance < amount)
            return ServiceResult<AccountResponseDto>.Failure("Insufficient funds.", 400);

        if (transferIntent.FromAccount.Id == transferIntent.ToAccount.Id)
            return ServiceResult<AccountResponseDto>.Failure("Source and destination accounts cannot be the same.", 400);

        if (amount <= 0)
            return ServiceResult<AccountResponseDto>.Failure("Amount must be greater than 0.", 400);

        if (transferIntent.FromAccount.Status != AccountStatus.Active)
            return ServiceResult<AccountResponseDto>.Failure("Source account is not active.", 400);

        if (transferIntent.ToAccount.Status != AccountStatus.Active)
            return ServiceResult<AccountResponseDto>.Failure("Destination account is not active.", 400);

        return ServiceResult<AccountResponseDto>.SuccessResult(transferIntent.FromAccount.Adapt<AccountResponseDto>(), "Transfer validated successfully.");
    }
}
