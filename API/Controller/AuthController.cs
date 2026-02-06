using API.Common;
using Microsoft.AspNetCore.Mvc;
using Application.Dtos;
namespace API.Controller
{
    
    public class AuthController() : BaseController
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            var result = await authService.RegisterAsync(request);
            if (!result.Success) return BadRequest(result.Message);
            return StatusCode(201, result.Data);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var result = await authService.LoginAsync(request);
            if (!result.Success) return Unauthorized(result.Message);
            return Ok(result.Data);
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(string refreshToken)
        {

            var result = await authService.RefreshToken(refreshToken);

            return result.Success ? Ok(result.Data) : Unauthorized(result.Message);
        }

    }
}
