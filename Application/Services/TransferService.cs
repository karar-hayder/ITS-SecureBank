using Application.Common.Models;
using Application.Interfaces;

namespace Application.Services;

public class TransferService: ITransferService
{
    private readonly IBankDbContext _context;
    

    public TransferService(IBankDbContext context)
    {
        _context = context;
    }

    public async Task<TransferResponse> TransferAsync(TransferRequest request)
    {
        await Task.CompletedTask; 
        return new TransferResponse
        {
            Success = false,
            Message = "Not implemented yet",
            ReferenceId = null
        };
    }
}
