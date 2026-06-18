using System.Security.Claims;
using PingMe.Services;

namespace PingMe.Middleware;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory; // ✅ Singleton-safe

    private static readonly HashSet<string> _trackedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST:/api/auth/login",
        "POST:/api/auth/logout",
        "DELETE:/api/groups",
        "POST:/api/groups",
        "DELETE:/api/groups/members",
        "POST:/api/blocks",
    };

    // ✅ Inject IServiceScopeFactory thay vì IAuditLogService trực tiếp
    public AuditLogMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var key = $"{context.Request.Method}:{context.Request.Path.Value?.ToLower()}";
        var shouldLog = _trackedActions.Any(a => key.StartsWith(a, StringComparison.OrdinalIgnoreCase));

        if (shouldLog && context.Response.StatusCode is >= 200 and < 300)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? context.User?.FindFirst("sub")?.Value;

            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString();

            var action = $"{context.Request.Method} {context.Request.Path}";
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var userIdInt = userId is null ? (int?)null : int.Parse(userId);

            // ✅ Tạo scope mới — DbContext fresh, không bị disposed
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                    await auditLog.LogAsync(userIdInt, action, ip, userAgent);
                }
                catch { /* silent */ }
            });
        }
    }
}