using Application.Common.Models;
using Application.Dtos;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthDtos.LoginResponseDto>> LoginAsync(AuthDtos.LoginDto request);
        Task<ServiceResult<AuthDtos.RefreshTokenDto>> RefreshToken(string refreshToken);
        Task<ServiceResult<AuthDtos.UserDto>> RegisterAsync(AuthDtos.RegisterDto request);
    }
}