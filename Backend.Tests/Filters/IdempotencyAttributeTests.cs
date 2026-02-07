using API.Filters;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Backend.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Backend.Tests.Filters;

public class IdempotencyAttributeTests
{
    private readonly BankDbContext _context;
    private readonly IdempotentAttribute _filter;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly ActionExecutingContext _actionContext;
    private readonly TestLogger _logger;

    public IdempotencyAttributeTests(ITestOutputHelper output)
    {
        _logger = new TestLogger(output);
        
        // Setup In-Memory DB
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new BankDbContext(options);

        // Setup Filter
        _filter = new IdempotentAttribute();

        // Setup Mocks
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IBankDbContext)))
            .Returns(_context); // Return concrete InMemory Context

        _httpContextMock = new Mock<HttpContext>();
        _httpContextMock.Setup(c => c.RequestServices).Returns(_serviceProviderMock.Object);

        // Setup Action Context
        var actionContext = new ActionContext(
            _httpContextMock.Object,
            new RouteData(),
            new ActionDescriptor()
        );

        _actionContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object()
        );
    }

    [Fact]
    public async Task Should_Ignore_When_Header_Missing()
    {
        // Arrange
        _logger.Log("Test: Should_Ignore_When_Header_Missing");
        _httpContextMock.Setup(c => c.Request.Headers).Returns(new HeaderDictionary());
        
        var nextCalled = false;
        ActionExecutionDelegate next = () => 
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(_actionContext, new List<IFilterMetadata>(), new object()));
        };

        // Act
        await _filter.OnActionExecutionAsync(_actionContext, next);

        // Assert
        _logger.Log($"Next Called: {nextCalled}");
        Assert.True(nextCalled);
        Assert.Null(_actionContext.Result);
    }

    [Fact]
    public async Task Should_Return_Cached_Response_When_Key_Exists()
    {
        // Arrange
        _logger.Log("Test: Should_Return_Cached_Response_When_Key_Exists");
        var key = "test-key-123";
        var userId = 1;
        var responseBody = "{\"message\": \"Cached Response\"}";

        _context.IdempotencyRecords.Add(new IdempotencyRecord 
        { 
            Key = key, 
            UserId = userId, 
            Path = "/test", 
            Method = "POST", 
            ResponseStatusCode = 200, 
            ResponseBody = responseBody 
        });
        await _context.SaveChangesAsync();

        SetupRequest(key, userId);

        ActionExecutionDelegate next = () => Task.FromResult<ActionExecutedContext>(null!); // Should not be called

        // Act
        await _filter.OnActionExecutionAsync(_actionContext, next);

        // Assert
        _logger.Log($"Result Type: {_actionContext.Result?.GetType().Name}");
        Assert.IsType<ContentResult>(_actionContext.Result);
        var result = (ContentResult)_actionContext.Result;
        Assert.Equal(responseBody, result.Content);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task Should_Execute_And_Save_When_Key_New()
    {
        // Arrange
        _logger.Log("Test: Should_Execute_And_Save_When_Key_New");
        var key = "new-key-456";
        var userId = 1;

        SetupRequest(key, userId);

        var executedContext = new ActionExecutedContext(_actionContext, new List<IFilterMetadata>(), new object())
        {
            Result = new ObjectResult(new { success = true }) { StatusCode = 201 }
        };

        ActionExecutionDelegate next = () => Task.FromResult(executedContext);

        // Act
        await _filter.OnActionExecutionAsync(_actionContext, next);

        // Assert
        var savedRecord = await _context.IdempotencyRecords.FirstOrDefaultAsync(x => x.Key == key);
        Assert.NotNull(savedRecord);
        Assert.Equal(201, savedRecord.ResponseStatusCode);
        _logger.Log($"Saved Record ID: {savedRecord.Id}");
    }

    private void SetupRequest(string key, int userId)
    {
        var headers = new HeaderDictionary { { "Idempotency-Key", key } };
        _httpContextMock.Setup(c => c.Request.Headers).Returns(headers);
        _httpContextMock.Setup(c => c.Request.Path).Returns("/test");
        _httpContextMock.Setup(c => c.Request.Method).Returns("POST");

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);
    }
}
