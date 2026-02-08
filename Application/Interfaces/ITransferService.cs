using Application.Common.Models;
using Application.DTOs;

namespace Application.Interfaces;

public interface ITransferService
{
    Task<ServiceResult<string>> InitiateTransferAsync(string fromAccountNumber, string toAccountNumber, int userId);
    Task<ServiceResult<AccountResponseDto>> CompleteTransferAsync(string intentId, decimal amount, string description, int userId);
    Task<ServiceResult<AccountResponseDto>> CancelTransferAsync(string intentId, int userId);
}
