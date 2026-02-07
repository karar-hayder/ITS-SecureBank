using Application.Common.Models;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

using Backend.Tests.Common;
using Xunit.Abstractions;

namespace Backend.Tests.Services;

public class AccountServiceTests
{
    private readonly BankDbContext _context;
    private readonly AccountService _service;
    private readonly TestLogger _logger;

    public AccountServiceTests(ITestOutputHelper output)
    {
        _logger = new TestLogger(output);
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BankDbContext(options);
        _service = new AccountService(_context);
    }

    [Fact]
    public async Task DepositAsync_ShouldIncreaseBalance()
    {
        // Arrange
        _logger.Log("Setup: Creating Account with 100 balance...");
        var account = new Account { AccountNumber = "DEP-001", Balance = 100m, UserId = 1, Status = AccountStatus.Active, AccountType = AccountType.Savings };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var request = new DepositDto(50m);
        _logger.Log("Action: Depositing 50...");

        // Act
        var result = await _service.DepositAsync(account.Id, request, 1);

        // Assert
        _logger.Log($"Result: Success={result.Success}, New Balance={account.Balance}");
        result.Success.Should().BeTrue();
        account.Balance.Should().Be(150m);

        var tx = await _context.Transactions.FirstOrDefaultAsync();
        tx.Should().NotBeNull();
        tx!.Type.Should().Be(TransactionType.Credit);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldDecreaseBalance()
    {
        // Arrange
        _logger.Log("Setup: Creating Account with 100 balance...");
        var account = new Account { AccountNumber = "WIT-001", Balance = 100m, UserId = 1, Status = AccountStatus.Active, AccountType = AccountType.Checking };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var request = new WithdrawDto(40m);
        _logger.Log("Action: Withdrawing 40...");

        // Act
        var result = await _service.WithdrawAsync(account.Id, request, 1);

        // Assert
        _logger.Log($"Result: Success={result.Success}, New Balance={account.Balance}");
        result.Success.Should().BeTrue();
        account.Balance.Should().Be(60m);

        var tx = await _context.Transactions.FirstOrDefaultAsync();
        tx.Should().NotBeNull();
        tx!.Type.Should().Be(TransactionType.Debit);
    }
}
