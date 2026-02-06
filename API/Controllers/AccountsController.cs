using API.Common;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class AccountsController(IAccountService accountService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetMyAccounts()
        {
            var result = await accountService.GetAccountsByUserIdAsync(UserId);
            return result.Success ? Ok(result.Data) : BadRequest(result.Message);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccount(int id)
        {
            var result = await accountService.GetAccountAsync(id);
            if (!result.Success) return NotFound(result.Message);
            
            // Security check: Ensure user owns the account
            if (result.Data!.UserId != UserId) return Forbid();
            
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(CreateAccountDto request)
        {
            // Security check: Ensure user can only create accounts for themselves
            if (request.UserId != UserId) return Forbid();
            
            var result = await accountService.CreateAccountAsync(request);
            return result.Success ? StatusCode(201, result.Data) : BadRequest(result.Message);
        }

        // [HttpGet("{id}/transactions")]
        // public async Task<IActionResult> GetTransactions(int id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        // {
        //     // Security check: Ensure user owns the account
        //     var accountResult = await accountService.GetAccountAsync(id);
        //     if (!accountResult.Success) return NotFound(accountResult.Message);
        //     if (accountResult.Data!.UserId != UserId) return Forbid();

        //     var result = await accountService.GetAccountTransactionsAsync(id, pageNumber, pageSize);
        //     return result.Success ? Ok(result.Data) : BadRequest(result.Message);
        // }
    }
}
