using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Backend.Tests.Common;
using System.Text;

namespace Backend.Tests.Services;

public class AccountApprovalServiceTests
{
    private readonly BankDbContext _context;
    private readonly AccountApprovalService _service;
    private readonly TestLogger _logger;

    public AccountApprovalServiceTests(ITestOutputHelper output)
    {
        _logger = new TestLogger(output);

        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new BankDbContext(options);

        _service = new AccountApprovalService(_context);
    }

    [Fact]
    public async Task RequestApprovalAsync_ShouldSucceed_WhenValid()
    {
        // Arrange
        _logger.Log("Test: RequestApprovalAsync_ShouldSucceed_WhenValid");
        var userId = 1;
        var account = new Account
        {
            Id = 10,
            AccountNumber = "PENDING-001",
            UserId = userId,
            Status = AccountStatus.Pending,
            AccountType = AccountType.Checking
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Mock IFormFile
        var fileMock = new Mock<IFormFile>();
        var content = "fake-file-content";
        var fileName = "id-card.jpg";
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);

        var request = new AccountApprovalRequestDto(account.Id, fileMock.Object);

        // Act
        var result = await _service.RequestApprovalAsync(request, userId);

        // Assert
        _logger.Log($"Result: Success={result.Success}, Message={result.Message}");
        result.Success.Should().BeTrue();

        var approvalRequest = await _context.AccountApprovalRequests.FirstOrDefaultAsync(r => r.AccountId == account.Id);
        approvalRequest.Should().NotBeNull();
        approvalRequest!.IdDocumentUrl.Should().Contain(fileName);
        _logger.Log($"Verified: Approval request created with file path containing {fileName}");
    }

    [Fact]
    public async Task ApproveOrRejectAccountAsync_ShouldActivateAccount_OnApproval()
    {
        // Arrange
        _logger.Log("Test: ApproveOrRejectAccountAsync_ShouldActivateAccount_OnApproval");
        var adminId = 99;
        var account = new Account { AccountNumber = "ACC-TO-APPROVE", Status = AccountStatus.Pending, UserId = 1, AccountType = AccountType.Checking };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var approvalRequest = new AccountApprovalRequest
        {
            AccountId = account.Id,
            UserId = 1,
            IdDocumentUrl = "some/path/id.jpg",
            IsApproved = false
        };
        _context.AccountApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        var request = new ApproveAccountDto(approvalRequest.Id, true, "Looks good.");

        // Act
        var result = await _service.ApproveOrRejectAccountAsync(request, adminId);

        // Assert
        _logger.Log($"Result: Success={result.Success}, Message={result.Message}");
        result.Success.Should().BeTrue();

        var updatedAccount = await _context.Accounts.FindAsync(account.Id);
        updatedAccount!.Status.Should().Be(AccountStatus.Active);

        var updatedRequest = await _context.AccountApprovalRequests.FindAsync(approvalRequest.Id);
        updatedRequest!.IsApproved.Should().BeTrue();
        updatedRequest.ProcessedByAdminId.Should().Be(adminId);
        _logger.Log("Verified: Account is now ACTIVE and request marked as Approved.");
    }
}
