
namespace Backend.API.Middleware;

public class RequestTimingHandler : IMiddleware
{
    private readonly ILogger<RequestTimingHandler> logger;

    public RequestTimingHandler(ILogger<RequestTimingHandler> logger)
    {
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var startTime = DateTime.UtcNow;

        context.Response.OnStarting(() =>
        {
            var duration = DateTime.UtcNow - startTime;
            context.Response.Headers.Append("X-Request-Duration", duration.TotalMilliseconds.ToString());
            return Task.CompletedTask;
        });

        await next(context);
        var totalDuration = DateTime.UtcNow - startTime;
        logger.LogInformation("Request duration: {Duration} ms", totalDuration.TotalMilliseconds);

        if (totalDuration.TotalMilliseconds > 1000)
        {
            logger.LogWarning("Request duration is too long: {Duration} ms", totalDuration.TotalMilliseconds);
        }
    }
}