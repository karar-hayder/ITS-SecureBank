using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using Backend.Tests.Common;
using Xunit.Abstractions;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Backend.Tests.Services;

public class TransferServiceTests
{
    private readonly BankDbContext _context;
    private readonly TransferService _service;
    private readonly Mock<ILogger<TransferService>> _loggerMock;
    private readonly TestLogger _logger;

    public TransferServiceTests(ITestOutputHelper output)
    {
        _logger = new TestLogger(output);
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new BankDbContext(options);
        _loggerMock = new Mock<ILogger<TransferService>>();
        _service = new TransferService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task TransferAsync_ShouldSucceed_WhenValid()
    {
        // Arrange
        _logger.Log("Setup: Creating Sender (1000) and Receiver (500)...");
        var sender = new Account { AccountNumber = "ACC-001", Balance = 1000m, UserId = 1, Status = AccountStatus.Active, AccountType = AccountType.Checking };
        var receiver = new Account { AccountNumber = "ACC-002", Balance = 500m, UserId = 2, Status = AccountStatus.Active, AccountType = AccountType.Checking };

        _context.Accounts.AddRange(sender, receiver);
        await _context.SaveChangesAsync();

        var request = new TransferDto("ACC-001", "ACC-002", 200m, "Test Transfer");
        _logger.Log($"Action: Transferring 200 from {sender.AccountNumber} to {receiver.AccountNumber}...");

        // Act
        var result = await _service.TransferAsync(request, 1);

        // Assert
        _logger.Log($"Result: Success={result.Success}, Message={result.Message}");
        result.Success.Should().BeTrue();

        sender.Balance.Should().Be(800m);
        receiver.Balance.Should().Be(700m);
        _logger.Log($"Verified: Sender Balance ({sender.Balance}) == 800, Receiver Balance ({receiver.Balance}) == 700");

        var transactions = await _context.Transactions.ToListAsync();
        transactions.Should().HaveCount(2);
        _logger.Log("Verified: 2 Ledger Entries created.");
    }

    [Fact]
    public async Task TransferAsync_ShouldFail_WhenInsufficientFunds()
    {
        // Arrange
        _logger.Log("Setup: Creating Sender with 100 balance...");
        var sender = new Account { AccountNumber = "ACC-003", Balance = 100m, UserId = 1, Status = AccountStatus.Active, AccountType = AccountType.Checking };
        var receiver = new Account { AccountNumber = "ACC-004", Balance = 500m, UserId = 2, Status = AccountStatus.Active, AccountType = AccountType.Checking };

        _context.Accounts.AddRange(sender, receiver);
        await _context.SaveChangesAsync();

        var request = new TransferDto("ACC-003", "ACC-004", 200m, "Test Transfer");
        _logger.Log("Action: Attempting to transfer 200...");

        // Act
        var result = await _service.TransferAsync(request, 1);

        // Assert
        _logger.Log($"Result: Success={result.Success}, Message={result.Message}");
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Insufficient funds");
        sender.Balance.Should().Be(100m);
        _logger.Log("Verified: Transfer failed and balance remained 100.");
    }

    [Fact]
    public async Task TransferAsync_ShouldFail_WhenSenderNotOwner()
    {
        // Arrange
        _logger.Log("Setup: Creating Sender owned by User 2...");
        var sender = new Account { AccountNumber = "ACC-005", Balance = 1000m, UserId = 2, Status = AccountStatus.Active, AccountType = AccountType.Checking };
        var receiver = new Account { AccountNumber = "ACC-006", Balance = 500m, UserId = 3, Status = AccountStatus.Active, AccountType = AccountType.Checking };

        _context.Accounts.AddRange(sender, receiver);
        await _context.SaveChangesAsync();

        var request = new TransferDto("ACC-005", "ACC-006", 200m, "Fraud");
        _logger.Log("Action: User 1 attempting to transfer from User 2's account...");

        // Act
        var result = await _service.TransferAsync(request, 1);

        // Assert
        _logger.Log($"Result: Success={result.Success}, StatusCode={result.StatusCode}");
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _logger.Log("Verified: Access Denied (403).");
    }
}
