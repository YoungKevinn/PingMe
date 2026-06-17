using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PingMe.Data;
using PingMe.DTOs.Block;
using PingMe.DTOs.Message;
using PingMe.Hubs;
using PingMe.Models;

namespace PingMe.Services;

public interface IBlockService
{
    Task<bool> IsBlockedAsync(int userId, int targetId);
    Task<(bool IsBlockedByMe, bool HasBlockedMe)> GetBlockStatusAsync(int userId, int targetId);
    Task<(bool Success, string? Error)> BlockUserAsync(int blockerId, int blockedId);
    Task<(bool Success, string? Error)> UnblockUserAsync(int blockerId, int blockedId);
    Task<List<BlockResponse>> GetBlockedUsersAsync(int userId);
}

public class BlockService : IBlockService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;
    public BlockService(AppDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<bool> IsBlockedAsync(int userId, int targetId) =>
        await _db.BlockedUsers.AnyAsync(b =>
            (b.BlockerUserId == userId && b.BlockedUserId == targetId) ||
            (b.BlockerUserId == targetId && b.BlockedUserId == userId));

    public async Task<(bool IsBlockedByMe, bool HasBlockedMe)> GetBlockStatusAsync(int userId, int targetId)
    {
        var records = await _db.BlockedUsers
            .AsNoTracking()
            .Where(b =>
                (b.BlockerUserId == userId && b.BlockedUserId == targetId) ||
                (b.BlockerUserId == targetId && b.BlockedUserId == userId))
            .Select(b => new { b.BlockerUserId, b.BlockedUserId })
            .ToListAsync();

        return (
            records.Any(b => b.BlockerUserId == userId && b.BlockedUserId == targetId),
            records.Any(b => b.BlockerUserId == targetId && b.BlockedUserId == userId));
    }

    public async Task<(bool Success, string? Error)> BlockUserAsync(int blockerId, int blockedId)
    {
        if (blockerId == blockedId) return (false, "Không thể tự block bản thân.");
        var exists = await _db.BlockedUsers.AnyAsync(b => b.BlockerUserId == blockerId && b.BlockedUserId == blockedId);
        if (exists) return (true, null);

        _db.BlockedUsers.Add(new BlockedUser { BlockerUserId = blockerId, BlockedUserId = blockedId });
        await _db.SaveChangesAsync();
        await AddDmSystemMessageAsync(blockerId, blockedId, $"{await GetDisplayNameAsync(blockerId)} đã chặn người này");
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnblockUserAsync(int blockerId, int blockedId)
    {
        var record = await _db.BlockedUsers.FirstOrDefaultAsync(b => b.BlockerUserId == blockerId && b.BlockedUserId == blockedId);
        if (record is null) return (false, "Chưa block user này.");
        _db.BlockedUsers.Remove(record);
        await _db.SaveChangesAsync();
        await AddDmSystemMessageAsync(blockerId, blockedId, $"{await GetDisplayNameAsync(blockerId)} đã bỏ chặn người này");
        return (true, null);
    }

    public async Task<List<BlockResponse>> GetBlockedUsersAsync(int userId)
    {
        return await _db.BlockedUsers
            .Where(b => b.BlockerUserId == userId)
            .Include(b => b.Blocked)
            .Select(b => new BlockResponse
            {
                BlockedUserId = b.BlockedUserId,
                DisplayName   = b.Blocked.DisplayName,
                AvatarUrl     = b.Blocked.AvatarUrl,
                CreatedAt     = b.CreatedAt
            }).ToListAsync();
    }

    private async Task<string> GetDisplayNameAsync(int userId)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync() ?? "Người dùng";
    }

    private async Task AddDmSystemMessageAsync(int senderId, int receiverId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            MessageType = MessageType.System,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var saved = await _db.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == message.Id);

        if (saved is null)
            return;

        var dto = new MessageResponse
        {
            Id = saved.Id,
            SenderId = saved.SenderId,
            SenderDisplayName = saved.Sender.DisplayName,
            SenderAvatarUrl = saved.Sender.AvatarUrl,
            ReceiverId = saved.ReceiverId,
            Content = saved.Content,
            MessageType = saved.MessageType.ToString(),
            CreatedAt = saved.CreatedAt,
            UpdatedAt = saved.UpdatedAt
        };

        await _hub.Clients.Group($"user_{senderId}").SendAsync("ReceiveMessage", dto);
        await _hub.Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", dto);
    }
}
