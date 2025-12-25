using System.Net;

namespace OrdersService.Api.Middleware;

public sealed class UserIdMiddleware
{
    private readonly RequestDelegate _next;

    public UserIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            if (!context.Request.Headers.TryGetValue(OrdersService.Api.Http.HttpHeaderNames.UserId, out var userId) || string.IsNullOrWhiteSpace(userId))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Missing {OrdersService.Api.Http.HttpHeaderNames.UserId} header.");
                return;
            }
        }

        await _next(context);
    }
}
