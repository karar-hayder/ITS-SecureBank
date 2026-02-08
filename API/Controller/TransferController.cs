using API.Common;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controller;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransferController(ITransferService transferService) : BaseController
{
    [HttpPost("intent")]
    public async Task<IActionResult> CreateTransferIntent([FromBody] CreateTransferIntentDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var result = await transferService.InitiateTransferAsync(request.FromAccountNumber, request.ToAccountNumber, userId.Value);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(new { transferIntentId = result.Data });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteTransfer([FromBody] CompleteTransferDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var result = await transferService.CompleteTransferAsync(request.TransferIntentId, request.Amount, request.Description, userId.Value);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result.Data);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelTransfer([FromBody] CancelTransferIntentDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var result = await transferService.CancelTransferAsync(request.TransferIntentId, userId.Value);

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

