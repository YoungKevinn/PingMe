using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.Services;

namespace PingMe.Middleware;

public class JwtRevocationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory; // ✅ Singleton-safe

    public JwtRevocationMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db, IJwtService jwtService)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var tokenHash = jwtService.HashToken(token);

            // ✅ Query 1: dùng db của request (scoped) — OK
            var session = await db.UserSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

            if (session is null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(
                    new { message = "Token không hợp lệ hoặc đã bị thu hồi." });
                return;
            }

            // ✅ Update LastActive dùng scope MỚI — không đụng vào db của request
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var freshDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await freshDb.UserSessions
                        .Where(s => s.TokenHash == tokenHash)
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(x => x.LastActive, DateTime.UtcNow));
                }
                catch { /* silent */ }
            });
        }

        // ✅ _next dùng db của request — không còn conflict
        await _next(context);
    }
}