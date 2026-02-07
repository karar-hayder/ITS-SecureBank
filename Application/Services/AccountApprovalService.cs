using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // For IFormFile

namespace Application.Services;

public class AccountApprovalService(IBankDbContext context) : IAccountApprovalService
{
    private const string UploadDirectory = "uploads/ids";

    public async Task<ServiceResult<string>> RequestApprovalAsync(AccountApprovalRequestDto request, int userId)
    {
        var account = await context.Accounts.FindAsync(request.AccountId);

        if (account == null)
            return ServiceResult<string>.Failure("Account not found.", 404);

        if (account.UserId != userId)
            return ServiceResult<string>.Failure("Unauthorized.", 403);

        if (account.Status != AccountStatus.Pending)
            return ServiceResult<string>.Failure("Account is not in pending status.", 400);

        // Check if request already exists
        var existingRequest = await context.AccountApprovalRequests
            .FirstOrDefaultAsync(r => r.AccountId == request.AccountId && r.ProcessedAt == null);

        if (existingRequest != null)
            return ServiceResult<string>.Failure("An approval request is already pending.", 400);

        // Simulate File Upload
        var filePath = await SaveFileAsync(request.IdDocument);

        var approvalRequest = new AccountApprovalRequest
        {
            AccountId = request.AccountId,
            UserId = userId,
            IdDocumentUrl = filePath,
            IsApproved = false
        };

        context.AccountApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();

        return ServiceResult<string>.SuccessResult("Approval request submitted successfully.");
    }

    public async Task<ServiceResult<string>> ApproveOrRejectAccountAsync(ApproveAccountDto request, int adminId)
    {
        // 1. Fetch Request
        var approvalRequest = await context.AccountApprovalRequests
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId);

        if (approvalRequest == null)
            return ServiceResult<string>.Failure("Request not found.", 404);

        if (approvalRequest.ProcessedAt != null)
            return ServiceResult<string>.Failure("Request already processed.", 400);

        // 2. Update Request Status
        approvalRequest.IsApproved = request.IsApproved;
        approvalRequest.ProcessedAt = DateTime.UtcNow;
        approvalRequest.ProcessedByAdminId = adminId;
        approvalRequest.AdminRemarks = request.Remarks;

        // 3. Update Account Status
        if (request.IsApproved)
        {
            approvalRequest.Account.Status = AccountStatus.Active;
        }
        else
        {
            // If rejected, we might want to keep it pending or move to Rejected/Closed
            // The requirement says "active or inactive or closed"
            approvalRequest.Account.Status = AccountStatus.Inactive;
        }

        context.AccountApprovalRequests.Update(approvalRequest);
        context.Accounts.Update(approvalRequest.Account);

        await context.SaveChangesAsync();

        var status = request.IsApproved ? "approved" : "rejected";
        return ServiceResult<string>.SuccessResult($"Account {status} successfully.");
    }

    public async Task<ServiceResult<PaginatedList<AccountApprovalRequestResponseDto>>> GetPendingRequestsAsync(int pageNumber, int pageSize)
    {
        var query = context.AccountApprovalRequests
            .Include(r => r.User)
            .Include(r => r.Account)
            .Where(r => r.ProcessedAt == null)
            .OrderByDescending(r => r.Id)
            .Select(r => new AccountApprovalRequestResponseDto(
                r.Id,
                r.AccountId,
                r.Account.AccountNumber,
                r.Account.AccountType,
                r.UserId,
                r.User.FullName,
                r.User.Email,
                r.IdDocumentUrl,
                r.Account.CreatedAt,
                r.IsApproved,
                r.ProcessedAt
            ));

        var list = await PaginatedList<AccountApprovalRequestResponseDto>.CreateAsync(query, pageNumber, pageSize);

        return ServiceResult<PaginatedList<AccountApprovalRequestResponseDto>>.SuccessResult(list);
    }

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        // In a real app, upload to Azure Blob Storage / AWS S3
        // Here we simulated by returning a fake path
        // Ensure directory exists
        if (!Directory.Exists(UploadDirectory))
        {
            Directory.CreateDirectory(UploadDirectory);
        }

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var path = Path.Combine(UploadDirectory, fileName);

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return path;
    }
}
