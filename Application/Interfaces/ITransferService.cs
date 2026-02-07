using Application.Common.Models;
using Application.DTOs;

namespace Application.Interfaces;

public interface ITransferService
{
    Task<ServiceResult<AccountResponseDto>> TransferAsync(TransferDto request, int userId);
}
