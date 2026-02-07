using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderKey = "Idempotency-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderKey, out var keyVals))
        {
            await next();
            return;
        }

        var key = keyVals.FirstOrDefault();
        if (string.IsNullOrEmpty(key))
        {
            context.Result = new BadRequestObjectResult(new { message = "Idempotency-Key cannot be empty." });
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IBankDbContext>();
        var userIdStr = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr))
        {
            await next();
            return;
        }
        int userId = int.Parse(userIdStr);
        string path = context.HttpContext.Request.Path;
        string method = context.HttpContext.Request.Method;

        var existing = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key && x.UserId == userId);

        if (existing != null)
        {
            context.Result = new ContentResult
            {
                Content = existing.ResponseBody,
                ContentType = "application/json",
                StatusCode = existing.ResponseStatusCode
            };
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult &&
            executedContext.Exception == null &&
            (objectResult.StatusCode >= 200 && objectResult.StatusCode < 500))
        {
            string jsonResponse = JsonSerializer.Serialize(objectResult.Value);

            var record = new IdempotencyRecord
            {
                Key = key,
                UserId = userId,
                Path = path,
                Method = method,
                ResponseStatusCode = objectResult.StatusCode ?? 200,
                ResponseBody = jsonResponse
            };

            dbContext.IdempotencyRecords.Add(record);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        else if (executedContext.Result is StatusCodeResult statusCodeResult &&
                 statusCodeResult.StatusCode >= 200 && statusCodeResult.StatusCode < 500)
        {
            var record = new IdempotencyRecord
            {
                Key = key,
                UserId = userId,
                Path = path,
                Method = method,
                ResponseStatusCode = statusCodeResult.StatusCode,
                ResponseBody = "{}"
            };

            dbContext.IdempotencyRecords.Add(record);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}
