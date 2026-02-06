using Application.Common.Models;
using Application.DTOs;

namespace Application.Interfaces;

public interface IAccountService
{
    Task<ServiceResult<AccountResponseDto>> CreateAccountAsync(CreateAccountDto request , int userId);
    Task<ServiceResult<AccountResponseDto>> GetAccountAsync(int id, int userId);
    Task<ServiceResult<AccountResponseDto>> UpdateAccountAsync(int id, UpdateAccountDto request);
    Task<ServiceResult<AccountResponseDto>> DeleteAccountAsync(int id);
    Task<ServiceResult<List<AccountResponseDto>>> GetAccountsByUserIdAsync(int userId);
    Task<ServiceResult<AccountResponseDto>> DepositAsync(int accountId, DepositDto request, int userId);
    Task<ServiceResult<AccountResponseDto>> WithdrawAsync(int accountId, WithdrawDto request, int userId);
}