using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces;

public interface IAccountApprovalService
{
    Task<ServiceResult<string>> RequestApprovalAsync(AccountApprovalRequestDto request, int userId);
    Task<ServiceResult<string>> ApproveOrRejectAccountAsync(ApproveAccountDto request, int adminId);
    Task<ServiceResult<PaginatedList<AccountApprovalRequestResponseDto>>> GetPendingRequestsAsync(int pageNumber, int pageSize);
}
