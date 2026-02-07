using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class AccountService(IBankDbContext context) : IAccountService
{
    public async Task<ServiceResult<AccountResponseDto>> CreateAccountAsync(CreateAccountDto request, int userId)
    {
        // Generate unique account number
        var accountNumber = GenerateAccountNumber();
        
        var account = new Account
        {
            AccountNumber = accountNumber,
            AccountType = request.AccountType,
            Level = (AccountLevel)1, // Default level for new accounts
            UserId = userId,
            Status = AccountStatus.Pending,
            Balance = 0m
        };
        
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        
        return ServiceResult<AccountResponseDto>.SuccessResult(
            account.Adapt<AccountResponseDto>(), 
            "Account created successfully.", 
            201);
    }

    public async Task<ServiceResult<AccountResponseDto>> GetAccountAsync(int id, int userId)
    {
        var account = await context.Accounts.FindAsync(id);
        
        if (account == null)
        {
            return ServiceResult<AccountResponseDto>.Failure("Account not found.", 404);
        }
        if (account.UserId != userId)
        {
            return ServiceResult<AccountResponseDto>.Failure("Unauthorized access to account.", 403);
        }

        return ServiceResult<AccountResponseDto>.SuccessResult(
            account.Adapt<AccountResponseDto>(), 
            "Account retrieved successfully.");
    }

    public async Task<ServiceResult<AccountResponseDto>> UpdateAccountAsync(int id, UpdateAccountDto request)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account == null)
        {
            return ServiceResult<AccountResponseDto>.Failure("Account not found.", 404);
        }

        request.Adapt(account);
        context.Accounts.Update(account);
        await context.SaveChangesAsync();
        
        return ServiceResult<AccountResponseDto>.SuccessResult(
            account.Adapt<AccountResponseDto>(), 
            "Account updated successfully.");
    }

    public async Task<ServiceResult<AccountResponseDto>> DeleteAccountAsync(int id)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account == null)
        {
            return ServiceResult<AccountResponseDto>.Failure("Account not found.", 404);
        }

        account.IsDeleted = true;
        context.Accounts.Update(account);
        await context.SaveChangesAsync();
        
        return ServiceResult<AccountResponseDto>.SuccessResult(
            account.Adapt<AccountResponseDto>(), 
            "Account deleted successfully.");
    }

    public async Task<ServiceResult<List<AccountResponseDto>>> GetAccountsByUserIdAsync(int userId)
    {
        var accounts = await context.Accounts
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .ToListAsync();
            
        return ServiceResult<List<AccountResponseDto>>.SuccessResult(
            accounts.Adapt<List<AccountResponseDto>>(), 
            "Accounts retrieved successfully.");
    }

    public async Task<ServiceResult<AccountResponseDto>> DepositAsync(int accountId, DepositDto request, int userId)
    {
        // Use optimistic concurrency control
        var account = await context.Accounts.FindAsync(accountId);
        
        if (account == null)
        {
            return ServiceResult<AccountResponseDto>.Failure("Account not found.", 404);
        }

        // Authorization check
        if (account.UserId != userId)
        {
            return ServiceResult<AccountResponseDto>.Failure("Unauthorized access to account.", 403);
        }

        // Check account status
        if (account.Status != AccountStatus.Active)
        {
            return ServiceResult<AccountResponseDto>.Failure("Account is not active.", 400);
        }

        try
        {
            // Update balance
            account.Balance += request.Amount;
            
            // Create transaction record
            var transaction = new Transaction
            {
                Type = TransactionType.Credit,
                Amount = request.Amount,
                AccountId = accountId,
                BalanceAfter = account.Balance,
                ReferenceId = Guid.NewGuid().ToString(),
                Description = $"Deposit of {request.Amount:C}"
            };

            context.Transactions.Add(transaction);
            context.Accounts.Update(account);
            
            await context.SaveChangesAsync();

            return ServiceResult<AccountResponseDto>.SuccessResult(
                account.Adapt<AccountResponseDto>(), 
                "Deposit successful.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<AccountResponseDto>.Failure(
                "Concurrency conflict. Please try again.", 
                409);
        }
    }

    public async Task<ServiceResult<AccountResponseDto>> WithdrawAsync(int accountId, WithdrawDto request, int userId)
    {
        // Use optimistic concurrency control
        var account = await context.Accounts.FindAsync(accountId);
        
        if (account == null)
        {
            return ServiceResult<AccountResponseDto>.Failure("Account not found.", 404);
        }

        // Authorization check
        if (account.UserId != userId)
        {
            return ServiceResult<AccountResponseDto>.Failure("Unauthorized access to account.", 403);
        }

        // Check account status
        if (account.Status != AccountStatus.Active)
        {
            return ServiceResult<AccountResponseDto>.Failure("Account is not active.", 400);
        }

        // Check sufficient funds
        if (account.Balance < request.Amount)
        {
            return ServiceResult<AccountResponseDto>.Failure("Insufficient funds.", 400);
        }

        try
        {
            // Update balance
            account.Balance -= request.Amount;
            
            // Create transaction record
            var transaction = new Transaction
            {
                Type = TransactionType.Debit,
                Amount = request.Amount,
                AccountId = accountId,
                BalanceAfter = account.Balance,
                ReferenceId = Guid.NewGuid().ToString(),
                Description = $"Withdrawal of {request.Amount:C}"
            };

            context.Transactions.Add(transaction);
            context.Accounts.Update(account);
            
            await context.SaveChangesAsync();

            return ServiceResult<AccountResponseDto>.SuccessResult(
                account.Adapt<AccountResponseDto>(), 
                "Withdrawal successful.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<AccountResponseDto>.Failure(
                "Concurrency conflict. Please try again.", 
                409);
        }
    }

    public async Task<ServiceResult<PaginatedList<TransactionResponseDto>>> GetTransactionsAsync(int accountId, int userId, int pageNumber, int pageSize)
    {
        var account = await context.Accounts.FindAsync(accountId);
        if (account == null)
        {
            return ServiceResult<PaginatedList<TransactionResponseDto>>.Failure("Account not found.", 404);
        }

        if (account.UserId != userId)
        {
            return ServiceResult<PaginatedList<TransactionResponseDto>>.Failure("Unauthorized access to account.", 403);
        }

        var query = context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ProjectToType<TransactionResponseDto>();

        var paginatedTransactions = await PaginatedList<TransactionResponseDto>.CreateAsync(query, pageNumber, pageSize);

        return ServiceResult<PaginatedList<TransactionResponseDto>>.SuccessResult(paginatedTransactions, "Transactions retrieved successfully.");
    }

    private static string GenerateAccountNumber()
    {
        var random = new Random();
        var countryCode = "IQ";
        var checkDigits = random.Next(10, 100).ToString("D2");
        var bankCode = "NTB"; // NanoTech Bank
        var accountNumber = random.Next(10000000, 99999999).ToString();
        var branchCode = random.Next(100000, 999999).ToString();

        return $"{countryCode}{checkDigits}{bankCode}{branchCode}{accountNumber}";

    }
}