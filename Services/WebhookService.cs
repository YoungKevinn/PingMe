using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Message;
using PingMe.DTOs.Webhook;
using PingMe.Models;

namespace PingMe.Services;

public interface IWebhookService
{
    Task<WebhookResponse> CreateWebhookAsync(int userId, CreateWebhookRequest request);
    Task<List<WebhookResponse>> GetGroupWebhooksAsync(int groupId, int userId);
    Task<(bool Success, string? Error)> DeleteWebhookAsync(int webhookId, int userId);
    Task<(bool Success, string? Error)> ToggleWebhookAsync(int webhookId, int userId, bool isActive);
    Task<(bool Success, string? Error)> ProcessIncomingAsync(string token, string signature, string rawBody, IncomingWebhookRequest request);
}

public class WebhookService : IWebhookService
{
    private readonly AppDbContext _db;
    private readonly IMessageService _msg;
    public WebhookService(AppDbContext db, IMessageService msg) { _db = db; _msg = msg; }

    public async Task<WebhookResponse> CreateWebhookAsync(int userId, CreateWebhookRequest request)
    {
        var wh = new Webhook
        {
            GroupId = request.GroupId, CreatedByUserId = userId, Name = request.Name,
            Token = Guid.NewGuid().ToString("N"),
            Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Webhooks.Add(wh);
        await _db.SaveChangesAsync();
        return MapToResponse(wh);
    }

    public async Task<List<WebhookResponse>> GetGroupWebhooksAsync(int groupId, int userId)
    {
        var isMember = await _db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (!isMember) return [];
        return await _db.Webhooks.Where(w => w.GroupId == groupId).Select(w => MapToResponse(w)).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> DeleteWebhookAsync(int webhookId, int userId)
    {
        var wh = await _db.Webhooks.FindAsync(webhookId);
        if (wh is null) return (false, "Webhook không tồn tại.");
        if (wh.CreatedByUserId != userId) return (false, "Không có quyền.");
        _db.Webhooks.Remove(wh);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleWebhookAsync(int webhookId, int userId, bool isActive)
    {
        var wh = await _db.Webhooks.FindAsync(webhookId);
        if (wh is null) return (false, "Webhook không tồn tại.");
        if (wh.CreatedByUserId != userId) return (false, "Không có quyền.");
        wh.IsActive = isActive; wh.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ProcessIncomingAsync(
        string token, string signature, string rawBody, IncomingWebhookRequest request)
    {
        var wh = await _db.Webhooks.FirstOrDefaultAsync(w => w.Token == token && w.IsActive);
        if (wh is null) return (false, "Webhook không tồn tại hoặc đã tắt.");

        var expected = $"sha256={Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(wh.Secret), Encoding.UTF8.GetBytes(rawBody))).ToLower()}";
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature)))
            return (false, "Signature không hợp lệ.");

        await _msg.SendMessageAsync(wh.CreatedByUserId, new SendMessageRequest
        {
            GroupId = wh.GroupId,
            Content = $"**{request.Username ?? "Webhook"}**: {request.Content}",
            MessageType = "System"
        });
        return (true, null);
    }

    private static WebhookResponse MapToResponse(Webhook w) => new()
    {
        Id = w.Id, GroupId = w.GroupId, Name = w.Name,
        Token = w.Token, Secret = w.Secret, IsActive = w.IsActive, CreatedAt = w.CreatedAt
    };
}
