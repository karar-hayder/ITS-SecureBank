using Application.Common.Models;
using Application.DTOs;

namespace Application.Interfaces;

public interface IAccountService
{
    Task<ServiceResult<AccountDto>> CreateAccountAsync(CreateAccountDto request);
    Task<ServiceResult<AccountDto>> GetAccountAsync(int id);
    Task<ServiceResult<AccountDto>> UpdateAccountAsync(int id, UpdateAccountDto request);
    Task<ServiceResult<AccountDto>> DeleteAccountAsync(int id);
    Task<ServiceResult<List<AccountDto>>> GetAccountsByUserIdAsync(int userId);
    // Task<ServiceResult<PaginatedList<TransactionDto>>> GetAccountTransactionsAsync(int accountId, int pageNumber, int pageSize);
}