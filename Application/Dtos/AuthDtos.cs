using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class AuthDtos
    {
        public record RegisterDto(string FirstName, string LastName, string Email, string Password);
        public record LoginDto(string Email, string Password);
        public record LoginResponseDto(string Token, RefreshTokenDto RefreshToken, UserDto User);
        public record RefreshTokenDto(string Token, string Refreshtoken, Guid UserId, DateTime ExpiresAt, bool IsRevoked);
        public record UserDto(Guid Id, string FirstName, string LastName, string Email);
    }
}
