using Application.Common.Models;
using Application.Interfaces;

namespace Application.Services;

public class TransferService(IBankDbContext context) : ITransferService
{
    public async Task<TransferResponse> TransferAsync(TransferRequest request)
    {
        // Implementation will follow the workflow design
        return new TransferResponse 
        { 
            Success = false, 
            Message = "Transfer implementation pending." 
        };
    }
}
