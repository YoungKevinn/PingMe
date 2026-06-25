using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.Services;

namespace PingMe.Middleware;

public class JwtRevocationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    // In-memory cache: tokenHash -> (isValid, cachedAt)
    // Entries auto-expire after 30 seconds
    private static readonly ConcurrentDictionary<string, (bool IsValid, DateTime CachedAt)> _tokenCache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

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

            // Check cache first — avoid DB round-trip for repeated requests with same token
            if (_tokenCache.TryGetValue(tokenHash, out var cached) && (DateTime.UtcNow - cached.CachedAt) < CacheDuration)
            {
                if (!cached.IsValid)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(
                        new { message = "Token không hợp lệ hoặc đã bị thu hồi." });
                    return;
                }
                // Token is valid and cached — skip DB query
            }
            else
            {
                // Cache miss or expired — query DB
                var session = await db.UserSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

                if (session is null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
                {
                    _tokenCache[tokenHash] = (false, DateTime.UtcNow);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(
                        new { message = "Token không hợp lệ hoặc đã bị thu hồi." });
                    return;
                }

                // Cache valid result
                _tokenCache[tokenHash] = (true, DateTime.UtcNow);

                // Update LastActive in background (fire-and-forget)
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

            // Periodically clean expired entries (every ~100 requests, non-blocking)
            if (_tokenCache.Count > 50 && Random.Shared.Next(100) == 0)
            {
                _ = Task.Run(() =>
                {
                    var cutoff = DateTime.UtcNow - CacheDuration;
                    foreach (var kvp in _tokenCache)
                    {
                        if (kvp.Value.CachedAt < cutoff)
                            _tokenCache.TryRemove(kvp.Key, out _);
                    }
                });
            }
        }

        await _next(context);
    }
}