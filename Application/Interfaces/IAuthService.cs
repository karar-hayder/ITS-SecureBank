using Application.Common.Models;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthDtos.UserDto>> RegisterAsync(AuthDtos.RegisterDto request);
        Task<ServiceResult<AuthDtos.LoginResponseDto>> LoginAsync(AuthDtos.LoginDto request);
        Task<ServiceResult<AuthDtos.RefreshTokenDto>> RefreshToken(string refreshToken);
    }
}