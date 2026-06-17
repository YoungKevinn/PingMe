using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Session;

namespace PingMe.Services;

public interface ISessionService
{
    Task<List<SessionResponse>> GetSessionsAsync(int userId, string currentTokenHash);
    Task<(bool Success, string? Error)> RevokeSessionAsync(int sessionId, int userId);
    Task RevokeAllOtherSessionsAsync(int userId, string currentTokenHash);
}

public class SessionService : ISessionService
{
    private readonly AppDbContext _db;
    public SessionService(AppDbContext db) => _db = db;

    public async Task<List<SessionResponse>> GetSessionsAsync(int userId, string currentTokenHash) =>
        await _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActive)
            .Select(s => new SessionResponse
            {
                Id = s.Id, DeviceInfo = s.DeviceInfo, IpAddress = s.IpAddress,
                LastActive = s.LastActive, CreatedAt = s.CreatedAt, ExpiresAt = s.ExpiresAt,
                IsCurrent = s.TokenHash == currentTokenHash
            }).ToListAsync();

    public async Task<(bool Success, string? Error)> RevokeSessionAsync(int sessionId, int userId)
    {
        var s = await _db.UserSessions.FindAsync(sessionId);
        if (s is null || s.UserId != userId) return (false, "Session không tồn tại.");
        s.IsRevoked = true;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task RevokeAllOtherSessionsAsync(int userId, string currentTokenHash) =>
        await _db.UserSessions
            .Where(s => s.UserId == userId && s.TokenHash != currentTokenHash)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRevoked, true));
}
