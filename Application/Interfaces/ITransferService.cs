using Application.Common.Models;

namespace Application.Interfaces;

public interface ITransferService
{
    Task<TransferResponse> TransferAsync(TransferRequest request);
}
