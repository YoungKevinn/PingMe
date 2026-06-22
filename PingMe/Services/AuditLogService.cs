using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.AuditLog;
using PingMe.Models;

namespace PingMe.Services;

public interface IAuditLogService
{
    Task LogAsync(int? userId, string action, string? ip, string? userAgent, object? metadata = null);
    Task<List<AuditLogResponse>> GetLogsAsync(int? userId, string? action, string? ip, DateTime? from, DateTime? to, int page, int pageSize);
    Task<AuditLogResponse?> GetByIdAsync(int id);
}

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    public AuditLogService(AppDbContext db) => _db = db;

    public async Task LogAsync(int? userId, string action, string? ip, string? userAgent, object? metadata = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId    = userId,
            Action    = action,
            IpAddress = ip,
            UserAgent = userAgent,
            Metadata  = metadata is null ? null : System.Text.Json.JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLogResponse>> GetLogsAsync(
        int? userId, string? action, string? ip,
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _db.AuditLogs.Include(a => a.User).AsQueryable();

        if (userId.HasValue)               query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));
        if (!string.IsNullOrEmpty(ip))     query = query.Where(a => a.IpAddress == ip);
        if (from.HasValue)                 query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)                   query = query.Where(a => a.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToResponse(a))
            .ToListAsync();
    }

    public async Task<AuditLogResponse?> GetByIdAsync(int id)
    {
        var log = await _db.AuditLogs.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
        return log is null ? null : MapToResponse(log);
    }

    private static AuditLogResponse MapToResponse(AuditLog a) => new()
    {
        Id              = a.Id,
        UserId          = a.UserId,
        UserDisplayName = a.User?.DisplayName,
        Action          = a.Action,
        IpAddress       = a.IpAddress,
        Metadata        = a.Metadata,
        CreatedAt       = a.CreatedAt
    };
}
