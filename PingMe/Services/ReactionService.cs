using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.Hubs;
using PingMe.Models;

namespace PingMe.Services;

public interface IReactionService
{
    Task<(bool Success, string? Error)> AddReactionAsync(int messageId, int userId, string emoji);
    Task<(bool Success, string? Error)> RemoveReactionAsync(int messageId, int userId, string emoji);
}

public class ReactionService : IReactionService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public ReactionService(AppDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<(bool Success, string? Error)> AddReactionAsync(int messageId, int userId, string emoji)
    {
        emoji = NormalizeEmoji(emoji);

        if (string.IsNullOrWhiteSpace(emoji))
            return (false, "Emoji không hợp lệ.");

        var message = await _db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

        if (message is null)
            return (false, "Tin nhắn không tồn tại.");

        if (!await CanAccessMessageAsync(userId, message))
            return (false, "Bạn không có quyền react tin nhắn này.");

        var exists = await _db.MessageReactions.AnyAsync(r =>
            r.MessageId == messageId &&
            r.UserId == userId &&
            r.Emoji == emoji);

        if (!exists)
        {
            _db.MessageReactions.Add(new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                foreach (var entry in ex.Entries)
                    entry.State = EntityState.Detached;
            }
        }

        await BroadcastReactionAsync(message, messageId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveReactionAsync(int messageId, int userId, string emoji)
    {
        emoji = NormalizeEmoji(emoji);

        if (string.IsNullOrWhiteSpace(emoji))
            return (false, "Emoji không hợp lệ.");

        var message = await _db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

        if (message is null)
            return (false, "Tin nhắn không tồn tại.");

        if (!await CanAccessMessageAsync(userId, message))
            return (false, "Bạn không có quyền bỏ reaction tin nhắn này.");

        var reaction = await _db.MessageReactions.FirstOrDefaultAsync(r =>
            r.MessageId == messageId &&
            r.UserId == userId &&
            r.Emoji == emoji);

        if (reaction is not null)
        {
            _db.MessageReactions.Remove(reaction);
            await _db.SaveChangesAsync();
        }

        await BroadcastReactionAsync(message, messageId);
        return (true, null);
    }

    private async Task<bool> CanAccessMessageAsync(int userId, Message message)
    {
        if (message.GroupId.HasValue)
        {
            return await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == message.GroupId.Value &&
                gm.UserId == userId &&
                !gm.Group.IsDeleted);
        }

        return message.SenderId == userId || message.ReceiverId == userId;
    }

    private async Task BroadcastReactionAsync(Message message, int messageId)
    {
        var reactions = await _db.MessageReactions
            .AsNoTracking()
            .Where(r => r.MessageId == messageId)
            .GroupBy(r => r.Emoji)
            .Select(g => new
            {
                Emoji = g.Key,
                Count = g.Count(),
                UserIds = g.Select(r => r.UserId).ToList()
            })
            .ToListAsync();

        var payload = new
        {
            messageId,
            groupId = message.GroupId,
            senderId = message.SenderId,
            receiverId = message.ReceiverId,
            reactions
        };

        if (message.GroupId.HasValue)
        {
            await _hub.Clients.Group($"group_{message.GroupId.Value}")
                .SendAsync("MessageReactionUpdated", payload);
        }
        else
        {
            await _hub.Clients.Group($"user_{message.SenderId}")
                .SendAsync("MessageReactionUpdated", payload);

            if (message.ReceiverId.HasValue)
            {
                await _hub.Clients.Group($"user_{message.ReceiverId.Value}")
                    .SendAsync("MessageReactionUpdated", payload);
            }
        }
    }

    private static string NormalizeEmoji(string? emoji)
    {
        emoji = emoji?.Trim() ?? string.Empty;

        if (emoji.Length > 10)
            emoji = emoji[..10];

        return emoji;
    }
}
