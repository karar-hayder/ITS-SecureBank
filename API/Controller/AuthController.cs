using API.Common;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using static Application.DTOs.AuthDtos;
using Application.Interfaces;
namespace API.Controller
{
    
    public class AuthController(IAuthService service) : BaseController
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            var result = await service.RegisterAsync(request);
            if (!result.Success) return BadRequest(result.Message);
            return StatusCode(201, result.Data);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var result = await service.LoginAsync(request);
            if (!result.Success) return Unauthorized(result.Message);
            return Ok(result.Data);
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(string refreshToken)
        {

            var result = await service.RefreshToken(refreshToken);

            return result.Success ? Ok(result.Data) : Unauthorized(result.Message);
        }

    }
}
