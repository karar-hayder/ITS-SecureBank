using Application.Common.Models;
using Domain.Entities;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Authentication;
using static Application.Dtos.AuthDtos;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class AuthService(IJwtTokenGenerator jwt, IBankDbContext context) : IAuthService
    {
        public async Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto request)
        {
            if (await context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return ServiceResult<UserDto>.Failure("Email already exists.");
            }


            var user = request.Adapt<User>();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);


            context.Users.Add(user);
            await context.SaveChangesAsync(CancellationToken.None);

            return ServiceResult<UserDto>.SuccessResult(user.Adapt<UserDto>(), "User registered successfully.", 201);
        }

        public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginDto request)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ServiceResult<LoginResponseDto>.Failure("Invalid email or password.", 401);
            }
            var token = jwt.GenerateToken(user);
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Refreshtoken = jwt.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsRevoked = false
            };
            context.RefreshTokens.Add(refreshToken);
            var refreshtoken = refreshToken.Adapt<RefreshTokenDto>();

            await context.SaveChangesAsync(CancellationToken.None);
            return ServiceResult<LoginResponseDto>.SuccessResult(new LoginResponseDto(token, refreshtoken, user.Adapt<UserDto>()));
        }

        public async Task<ServiceResult<RefreshTokenDto>> RefreshToken(string refreshToken)
        {
            var storedToken = await context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Refreshtoken == refreshToken);

            if (storedToken == null ||
                storedToken.IsRevoked ||
                storedToken.ExpiresAt < DateTime.UtcNow)
                return ServiceResult<RefreshTokenDto>.Failure("Invalid refresh Token.", 401);

            var user = await context.Users.FindAsync(storedToken.UserId);

            if (user == null)
                return ServiceResult<RefreshTokenDto>.Failure("User not found.", 404);
            var newAccessToken = jwt.GenerateToken(user);
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Refreshtoken = jwt.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsRevoked = false
            };

            storedToken.IsRevoked = true;

            context.RefreshTokens.Add(newRefreshToken);

            await context.SaveChangesAsync(CancellationToken.None);

            var response = newRefreshToken.Adapt<RefreshTokenDto>()
            with
            { Token = newAccessToken };
            return response != null
                ? ServiceResult<RefreshTokenDto>.SuccessResult(response)
                : ServiceResult<RefreshTokenDto>.Failure("Could not refresh token.", 500);
        }
    }
}
