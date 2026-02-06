using API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces;
using System.Security.Claims;

namespace API.Controller;

[Authorize]
public class AccountController(IAccountService accountService) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateAccount(CreateAccountDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var result = await accountService.CreateAccountAsync(request, userId.Value);
        
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var result = await accountService.GetAccountsByUserIdAsync(userId.Value);
        
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result.Data);
    }

    [HttpPost("{id}/deposit")]
    public async Task<IActionResult> Deposit(int id, DepositDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var result = await accountService.DepositAsync(id, request, userId.Value);
        
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result.Data);
    }

    [HttpPost("{id}/withdraw")]
    public async Task<IActionResult> Withdraw(int id, WithdrawDto request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return Unauthorized("User not authenticated.");
        }

        var result = await accountService.WithdrawAsync(id, request, userId.Value);
        
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
