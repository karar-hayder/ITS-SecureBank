using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class AccountService(IBankDbContext context) : IAccountService
{
    public async Task<ServiceResult<AccountDto>> CreateAccountAsync(CreateAccountDto request)
    {
        var account = request.Adapt<Account>();
        // Ensure account number is generated - this should ideally be in a domain service
        account.AccountNumber = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
        
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        
        return ServiceResult<AccountDto>.SuccessResult(account.Adapt<AccountDto>(), "Account created successfully.", 201);
    }

    public async Task<ServiceResult<AccountDto>> GetAccountAsync(int id)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account == null)
        {
            return ServiceResult<AccountDto>.Failure("Account not found.", 404);
        }
        return ServiceResult<AccountDto>.SuccessResult(account.Adapt<AccountDto>(), "Account retrieved successfully.");
    }

    public async Task<ServiceResult<AccountDto>> UpdateAccountAsync(int id, UpdateAccountDto request)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account == null)
        {
            return ServiceResult<AccountDto>.Failure("Account not found.", 404);
        }

        request.Adapt(account);
        context.Accounts.Update(account);
        await context.SaveChangesAsync();
        
        return ServiceResult<AccountDto>.SuccessResult(account.Adapt<AccountDto>(), "Account updated successfully.");
    }

    public async Task<ServiceResult<AccountDto>> DeleteAccountAsync(int id)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account == null)
        {
            return ServiceResult<AccountDto>.Failure("Account not found.", 404);
        }

        context.Accounts.Remove(account);
        await context.SaveChangesAsync();
        
        return ServiceResult<AccountDto>.SuccessResult(account.Adapt<AccountDto>(), "Account deleted successfully.");
    }

    public async Task<ServiceResult<List<AccountDto>>> GetAccountsByUserIdAsync(int userId)
    {
        var accounts = await context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();
            
        return ServiceResult<List<AccountDto>>.SuccessResult(accounts.Adapt<List<AccountDto>>(), "Accounts retrieved successfully.");
    }
}