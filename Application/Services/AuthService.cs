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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class AuthService(
        IJwtTokenGenerator jwt, 
        IBankDbContext context, 
        UserManager<User> userManager,
        IAccountService accountService) : IAuthService
    {
        public async Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto request)
        {
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return ServiceResult<UserDto>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Requirement 4.1: Automatically provisions a default Checking account
            await accountService.CreateAccountAsync(new CreateAccountDto(
                AccountType: AccountType.Checking,
                UserId: user.Id,
                Level: AccountLevel.Level1
            ));

            return ServiceResult<UserDto>.SuccessResult(user.Adapt<UserDto>(), "User registered successfully.", 201);
        }

        public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginDto request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
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
            await context.SaveChangesAsync(CancellationToken.None);

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

            var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());

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
            context.RefreshTokens.Add(newRefreshToken);
            await context.SaveChangesAsync(CancellationToken.None);

            var response = newRefreshToken.Adapt<RefreshTokenDto>() with { Token = newAccessToken };
            return ServiceResult<RefreshTokenDto>.SuccessResult(response);
        }
    }
}
