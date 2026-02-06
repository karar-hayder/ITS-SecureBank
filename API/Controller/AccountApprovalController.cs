using API.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controller;

[Authorize]
public class AccountApprovalController(IAccountApprovalService approvalService) : BaseController
{
    // User: Request Approval
    [HttpPost("request")]
    public async Task<IActionResult> RequestApproval([FromForm] AccountApprovalRequestDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized("User not authenticated.");

        var result = await approvalService.RequestApprovalAsync(request, userId.Value);
        
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(new { message = result.Data });
    }

    // Admin: Get Pending Requests
    [HttpGet("admin/pending")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // if (!IsAdmin()) return Forbid("Access denied. Admin only.");

        var result = await approvalService.GetPendingRequestsAsync(page, pageSize);
        
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    // Admin: Approve/Reject
    [HttpPost("admin/decide")]
    public async Task<IActionResult> ApproveOrReject([FromBody] ApproveAccountDto request)
    {
        var adminId = GetUserIdFromClaims();
        // if (adminId == null || !IsAdmin()) return Forbid("Access denied. Admin only.");

        var result = await approvalService.ApproveOrRejectAccountAsync(request, adminId.Value);

        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(new { message = result.Data });
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userIdClaim) ? null : int.Parse(userIdClaim);
    }

    private bool IsAdmin()
    {
        // Check for role claim. 
        // Assuming the JWT token generator puts the Role in ClaimTypes.Role
        // Since the ClaimTypes.Role is standard, we check it.
        // We'll also cross-verify with enum string name "Admin"
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim == UserRole.Admin.ToString();
    }
}
