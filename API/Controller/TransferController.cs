using API.Common;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controller;

[Authorize]
public class TransferController(ITransferService transferService) : BaseController
{
    [HttpPost]
    [TypeFilter(typeof(API.Filters.IdempotentAttribute))]
    public async Task<IActionResult> Transfer([FromBody] TransferDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var result = await transferService.TransferAsync(request, userId.Value);
        
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result.Data);
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return null;
        }

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
