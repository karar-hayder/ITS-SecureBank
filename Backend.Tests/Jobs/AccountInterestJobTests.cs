using API.Jobs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Backend.Tests.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Backend.Tests.Jobs;

public class AccountInterestJobTests
{
    private readonly BankDbContext _context;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<AccountInterestJob>> _loggerMock;
    private readonly AccountInterestJob _job;
    private readonly TestLogger _logger;

    public AccountInterestJobTests(ITestOutputHelper output)
    {
        _logger = new TestLogger(output);

        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new BankDbContext(options);

        _loggerMock = new Mock<ILogger<AccountInterestJob>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        // Setup Scope
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(scopeMock.Object);

        scopeMock
            .Setup(x => x.ServiceProvider.GetService(typeof(IBankDbContext)))
            .Returns(_context);

        _job = new AccountInterestJob(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ApplyInterestAsync_ShouldAddInterest_ToSavingsAccounts()
    {
        // Arrange
        _logger.Log("Test: ApplyInterestAsync_ShouldAddInterest_ToSavingsAccounts");
        var account = new Account
        {
            AccountNumber = "SAVE-001",
            Balance = 1000m,
            AccountType = AccountType.Savings,
            Status = AccountStatus.Active,
            UserId = 1
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Use reflection to call private method
        var method = typeof(AccountInterestJob).GetMethod("ApplyInterestAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        _logger.Log("Action: Invoking private ApplyInterestAsync via reflection...");
        await (Task)method!.Invoke(_job, new object[] { CancellationToken.None })!;

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        // Interest rate is 0.0001 (0.01%). 1000 * 0.0001 = 0.10.
        updatedAccount!.Balance.Should().Be(1000.10m);

        var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.AccountId == account.Id);
        tx.Should().NotBeNull();
        tx!.Amount.Should().Be(0.10m);
        tx.Type.Should().Be(TransactionType.Credit);
        _logger.Log($"Verified: Balance increased to {updatedAccount.Balance} and Interest Transaction created.");
    }

    [Fact]
    public async Task ApplyInterestAsync_ShouldIgnore_NonSavingsAccounts()
    {
        // Arrange
        _logger.Log("Test: ApplyInterestAsync_ShouldIgnore_NonSavingsAccounts");
        var account = new Account
        {
            AccountNumber = "CHECK-001",
            Balance = 1000m,
            AccountType = AccountType.Checking,
            Status = AccountStatus.Active,
            UserId = 1
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var method = typeof(AccountInterestJob).GetMethod("ApplyInterestAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        await (Task)method!.Invoke(_job, new object[] { CancellationToken.None })!;

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount!.Balance.Should().Be(1000m);
        _logger.Log("Verified: Checking account balance remained 1000.");
    }
}
