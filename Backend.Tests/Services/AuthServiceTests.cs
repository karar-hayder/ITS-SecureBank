using Application.Common.Models;
using Application.DTOs;
using static Application.DTOs.AuthDtos;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Backend.Tests.Common;

namespace Backend.Tests.Services;

public class AuthServiceTests
{
    private readonly BankDbContext _context;
    private readonly AuthService _service;
    private readonly Mock<IJwtTokenGenerator> _jwtMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly TestLogger _logger;

    public AuthServiceTests(ITestOutputHelper output)
    {
        _logger = new TestLogger(output);

        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new BankDbContext(options);

        _jwtMock = new Mock<IJwtTokenGenerator>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _service = new AuthService(_jwtMock.Object, _context, _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSucceed_WhenValid()
    {
        // Arrange
        _logger.Log("Test: RegisterAsync_ShouldSucceed_WhenValid");
        // Order: FullName, Email, PhoneNumber, Password
        var request = new RegisterDto("Test User", "test@example.com", "1234567890", "Password123!");

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        _logger.Log($"Result: Success={result.Success}, Message={result.Message}");
        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be(request.Email);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        userInDb.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify(request.Password, userInDb!.PasswordHash).Should().BeTrue();
        _logger.Log("Verified: User saved in DB with hashed password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldSucceed_WithValidCredentials()
    {
        // Arrange
        _logger.Log("Test: LoginAsync_ShouldSucceed_WithValidCredentials");
        var email = "login@example.com";
        var password = "Password123!";
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Login User",
            PhoneNumber = "1112223333"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _jwtMock.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("fake-jwt-token");
        _jwtMock.Setup(x => x.GenerateRefreshToken()).Returns("fake-refresh-token");

        var request = new LoginDto(email, password);

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        _logger.Log($"Result: Success={result.Success}, Token={result.Data?.Token}");
        result.Success.Should().BeTrue();
        result.Data!.Token.Should().Be("fake-jwt-token");
        result.Data.RefreshToken.Refreshtoken.Should().Be("fake-refresh-token");

        var tokenInDb = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);
        tokenInDb.Should().NotBeNull();
        _logger.Log("Verified: Refresh token stored in DB.");
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WithInvalidPassword()
    {
        // Arrange
        _logger.Log("Test: LoginAsync_ShouldFail_WithInvalidPassword");
        var email = "wrong@example.com";
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            FullName = "Wrong User",
            PhoneNumber = "000"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginDto(email, "WrongPassword");

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        _logger.Log($"Result: Success={result.Success}, StatusCode={result.StatusCode}");
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }
}
