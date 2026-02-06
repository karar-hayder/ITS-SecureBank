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
        // 1. Check Header
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderKey, out var keyVals))
        {
            await next(); // Optional idempotent behavior if header missing
            return;
        }

        var key = keyVals.FirstOrDefault();
        if (string.IsNullOrEmpty(key))
        {
             context.Result = new BadRequestObjectResult(new { message = "Idempotency-Key cannot be empty." });
             return;
        }

        // 2. Get Services
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IBankDbContext>();
        var userIdStr = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdStr)) 
        {
            await next(); 
            return; // Only works for authenticated users
        }
        int userId = int.Parse(userIdStr);
        string path = context.HttpContext.Request.Path;
        string method = context.HttpContext.Request.Method;

        // 3. Check for existing key
        var existing = await dbContext.IdempotencyRecords
            .AsNoTracking() // Important for performance
            .FirstOrDefaultAsync(x => x.Key == key && x.UserId == userId);

        if (existing != null)
        {
            // Return saved response
            // Need to deserialize generic object or return raw JSON?
            // Since we can't easily reconstruct IActionResult from string, we return ContentResult with JSON
           
           context.Result = new ContentResult
           {
               Content = existing.ResponseBody,
               ContentType = "application/json",
               StatusCode = existing.ResponseStatusCode
           };
           return;
        }

        // 4. Execute Action
        var executedContext = await next();

        // 5. Save Response
        if (executedContext.Result is ObjectResult objectResult && 
            executedContext.Exception == null && // Only save if successful? Requirement: "Replaying... must not duplicate effects". 
                                                  // Even if it failed, we might want to return same failure?
                                                  // Usually we retry 500s, but 400s are saved. 
                                                  // For safely, let's save 2xx and 4xx.
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
            await dbContext.SaveChangesAsync(CancellationToken.None); // Use independent token
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
