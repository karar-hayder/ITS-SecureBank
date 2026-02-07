using Application.Common.Models;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Provides two-step transfer functionality:
/// 1. InitiateTransferAsync - Starts a transfer and returns an idempotency key.
/// 2. CompleteTransferAsync - Completes the transfer using the key and amount.
/// </summary>
public interface ITransferService
{
    Task<ServiceResult<string>> InitiateTransferAsync(string fromAccountNumber, string toAccountNumber, int userId);
    Task<ServiceResult<AccountResponseDto>> CompleteTransferAsync(string intentId, decimal amount, string description, int userId);
}
