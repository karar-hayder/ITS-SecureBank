using Application.Common.Models;
using Domain.Entities;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Authentication;
using static Application.DTOs.AuthDtos;
using Application.Interfaces;
using Application.DTOs;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class AuthService(
        IJwtTokenGenerator jwt, 
        IBankDbContext context, 
        ILogger<AuthService> logger) : IAuthService
    {
        public async Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto request)
        {
            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await context.Users.AddAsync(user);
            var saved = await context.SaveChangesAsync();

            if (saved == 0)
            {
                return ServiceResult<UserDto>.Failure("User registration failed.", 500);
            }
            logger.LogInformation("User registered successfully: {User}", user);
            return ServiceResult<UserDto>.SuccessResult(user.Adapt<UserDto>(), "User registered successfully.", 201);
        }

        public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginDto request)
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            
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

            await context.RefreshTokens.AddAsync(refreshToken);
            await context.SaveChangesAsync();

            var refreshtokenDto = refreshToken.Adapt<RefreshTokenDto>();
            return ServiceResult<LoginResponseDto>.SuccessResult(new LoginResponseDto(token, refreshtokenDto, user.Adapt<UserDto>()));
        }

        public async Task<ServiceResult<RefreshTokenDto>> RefreshToken(string refreshToken)
        {
            var storedToken = await context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Refreshtoken == refreshToken);

            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return ServiceResult<RefreshTokenDto>.Failure("Invalid refresh Token.", 401);
            }

            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == storedToken.UserId);

            if (user == null)
            {
                return ServiceResult<RefreshTokenDto>.Failure("User not found.", 404);
            }

            var newAccessToken = jwt.GenerateToken(user);
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Refreshtoken = jwt.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsRevoked = false
            };

            storedToken.IsRevoked = true;
            await context.RefreshTokens.AddAsync(newRefreshToken);
            await context.SaveChangesAsync();

            var response = newRefreshToken.Adapt<RefreshTokenDto>() with { Token = newAccessToken };
            return ServiceResult<RefreshTokenDto>.SuccessResult(response);
        }
    }
}
